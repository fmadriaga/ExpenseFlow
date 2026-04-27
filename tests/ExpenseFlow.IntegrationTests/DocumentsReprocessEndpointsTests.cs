using System.Net;
using System.Security.Cryptography;
using ExpenseFlow.Application.Ocr;
using ExpenseFlow.Domain.Entities;
using ExpenseFlow.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ExpenseFlow.IntegrationTests;

[CollectionDefinition("ReprocessApi serial", DisableParallelization = true)]
public sealed class ReprocessApiSerialCollection;

/// <summary>
/// Host de prueba sin seed previo: cada test prepara su propia BD.
/// </summary>
[Collection("ReprocessApi serial")]
public sealed class ReprocessApiTestFactory : WebApplicationFactory<Program>
{
    public string DbFile { get; }

    public string SqliteConnectionString { get; }

    private readonly string _tempRoot;

    public string TempRoot => _tempRoot;

    public ReprocessApiTestFactory()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), $"expenseflow_reprocess_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempRoot);
        DbFile = Path.GetFullPath(Path.Combine(_tempRoot, "reprocess.db"));
        SqliteConnectionString = new SqliteConnectionStringBuilder { DataSource = DbFile }
            .ConnectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var inbox = Path.Combine(_tempRoot, "inbox");
        var processed = Path.Combine(_tempRoot, "processed");
        var error = Path.Combine(_tempRoot, "error");
        Directory.CreateDirectory(inbox);
        Directory.CreateDirectory(processed);
        Directory.CreateDirectory(error);

        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:ExpenseFlow", SqliteConnectionString);
        builder.ConfigureAppConfiguration(
            (_, config) =>
            {
                config.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:ExpenseFlow"] = SqliteConnectionString,
                        ["Storage:Inbox"] = inbox,
                        ["Storage:Processed"] = processed,
                        ["Storage:Error"] = error,
                    });
            });
    }
}

public class DocumentsReprocessEndpointsTests : IClassFixture<ReprocessApiTestFactory>, IAsyncLifetime
{
    private readonly ReprocessApiTestFactory _factory;
    private HttpClient _client = null!;
    private DbContextOptions<ExpenseFlowDbContext> _options = null!;

    public DocumentsReprocessEndpointsTests(ReprocessApiTestFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        _options = new DbContextOptionsBuilder<ExpenseFlowDbContext>()
            .UseSqlite(_factory.SqliteConnectionString)
            .Options;
        await using (var db = new ExpenseFlowDbContext(_options))
        {
            await db.Database.EnsureDeletedAsync();
            await db.Database.MigrateAsync();
            await SyncFamilyPathsToTempStorageAsync(db);
        }

        SqliteConnection.ClearAllPools();
        _client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
        SqliteConnection.ClearAllPools();
        if (File.Exists(_factory.DbFile))
        {
            try
            {
                File.Delete(_factory.DbFile);
            }
            catch
            {
                // best effort
            }
        }

        if (Directory.Exists(_factory.TempRoot))
        {
            try
            {
                Directory.Delete(_factory.TempRoot, recursive: true);
            }
            catch
            {
                // best effort
            }
        }
    }

    private async Task ResetDatabaseAsync()
    {
        await using var db = new ExpenseFlowDbContext(_options);
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
        await SyncFamilyPathsToTempStorageAsync(db);
        SqliteConnection.ClearAllPools();
    }

    private async Task SyncFamilyPathsToTempStorageAsync(ExpenseFlowDbContext db)
    {
        var inbox = Path.Combine(_factory.TempRoot, "inbox");
        var processed = Path.Combine(_factory.TempRoot, "processed");
        var error = Path.Combine(_factory.TempRoot, "error");
        var fam = await db.Families.FirstAsync(f => f.Id == 1);
        fam.InboxPath = inbox;
        fam.ProcessedPath = processed;
        fam.ErrorPath = error;
        await db.SaveChangesAsync();
    }

    private static string Sha256Hex(byte[] data) =>
        Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant();

    /// <summary>
    /// Un solo [Fact]: xUnit instancia la clase por método y IAsyncLifetime se ejecutaría varias veces
    /// sobre el mismo host/SQLite (bloqueos). Secuencia 404 → 422 → 200 con ResetDatabase entre escenarios.
    /// </summary>
    [Fact]
    public async Task Post_reprocess_returns_404_422_and_200_with_pending_and_file_move()
    {
        await ResetDatabaseAsync();
        var r404 = await _client.PostAsync(new Uri("/documents/999999/reprocess", UriKind.Relative), null);
        Assert.Equal(HttpStatusCode.NotFound, r404.StatusCode);

        await ResetDatabaseAsync();
        var createdAt = new DateTimeOffset(2026, 4, 1, 12, 0, 0, TimeSpan.Zero);
        int successDocId;
        await using (var db = new ExpenseFlowDbContext(_options))
        {
            var doc = new Document
            {
                FamilyId = 1,
                FilePath = "/inbox/x.png",
                FileHash = "ab" + new string('0', 62),
                OcrStatus = ReceiptOcrStatuses.Success,
                CreatedAt = createdAt,
            };
            db.Documents.Add(doc);
            await db.SaveChangesAsync();
            successDocId = doc.Id;
        }

        var r422 = await _client.PostAsync(new Uri($"/documents/{successDocId}/reprocess", UriKind.Relative), null);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, r422.StatusCode);

        await ResetDatabaseAsync();
        var payload = new byte[] { 9, 8, 7, 6, 5 };
        var hash = Sha256Hex(payload);
        var errorDir = Path.Combine(_factory.TempRoot, "error", "2026", "04");
        Directory.CreateDirectory(errorDir);
        var errorFile = Path.Combine(errorDir, "lost.png");
        await File.WriteAllBytesAsync(errorFile, payload);

        int failedDocId;
        await using (var db2 = new ExpenseFlowDbContext(_options))
        {
            var doc = new Document
            {
                FamilyId = 1,
                FilePath = "/inbox/original.png",
                FileHash = hash,
                OcrStatus = ReceiptOcrStatuses.Failed,
                ErrorMessage = "ocr_failed",
                CreatedAt = createdAt,
            };
            db2.Documents.Add(doc);
            await db2.SaveChangesAsync();
            failedDocId = doc.Id;
        }

        var r200 = await _client.PostAsync(new Uri($"/documents/{failedDocId}/reprocess", UriKind.Relative), null);
        Assert.Equal(HttpStatusCode.OK, r200.StatusCode);

        await using (var db3 = new ExpenseFlowDbContext(_options))
        {
            var reloaded = await db3.Documents.FindAsync(failedDocId);
            Assert.NotNull(reloaded);
            Assert.Equal(ReceiptOcrStatuses.Pending, reloaded!.OcrStatus);
            Assert.Null(reloaded.ErrorMessage);
        }

        Assert.False(File.Exists(errorFile));
        var inboxFiles = Directory.GetFiles(Path.Combine(_factory.TempRoot, "inbox"));
        Assert.Single(inboxFiles);
        await using (var fs = new FileStream(inboxFiles[0], FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            var read = new byte[payload.Length];
            Assert.Equal(payload.Length, await fs.ReadAsync(read));
            Assert.Equal(payload, read);
        }
    }
}
