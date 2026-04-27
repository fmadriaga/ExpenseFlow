using System.Net;
using System.Text;
using ExpenseFlow.Domain.Entities;
using ExpenseFlow.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace ExpenseFlow.IntegrationTests;

/// <summary>
/// Host de prueba con seed orientado a CSV (comercio con comillas y coma).
/// </summary>
public sealed class DocumentsExportApiFactory : WebApplicationFactory<Program>
{
    public readonly string DbFile = Path.GetFullPath(
        Path.Combine(
            Path.GetTempPath(),
            $"expenseflow_api_export_test_{Guid.NewGuid():N}.db"));

    public string SqliteConnectionString { get; }

    private readonly string _tempRoot = Path.Combine(
        Path.GetTempPath(),
        $"expenseflow_api_export_storage_{Guid.NewGuid():N}");

    public string TempRoot => _tempRoot;

    public DocumentsExportApiFactory()
    {
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

[CollectionDefinition("DocumentsExportApi serial", DisableParallelization = true)]
public sealed class DocumentsExportApiSerialCollection;

[Collection("DocumentsExportApi serial")]
public sealed class DocumentsExportEndpointsTests : IClassFixture<DocumentsExportApiFactory>, IAsyncLifetime
{
    private readonly DocumentsExportApiFactory _factory;
    private HttpClient _client = null!;
    private DbContextOptions<ExpenseFlowDbContext> _options = null!;

    public DocumentsExportEndpointsTests(DocumentsExportApiFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<ExpenseFlowDbContext>()
            .UseSqlite(_factory.SqliteConnectionString)
            .Options;
        _options = options;

        await using (var db = new ExpenseFlowDbContext(options))
        {
            await db.Database.EnsureDeletedAsync();
            if (!await db.Database.EnsureCreatedAsync())
            {
                throw new InvalidOperationException("EnsureCreated did not create the database schema.");
            }

            var createdAt = new DateTimeOffset(2026, 4, 1, 12, 0, 0, TimeSpan.Zero);
            var document = new Document
            {
                FilePath = "/inbox/ticket.png",
                FileHash = "e3e3e3e3e3e3e3e3e3e3e3e3e3e3e3e3",
                RawJson = """{"test":true}""",
                OcrStatus = "Success",
                ErrorMessage = null,
                MerchantName = "Mini Market, \"Express\"",
                TransactionDate = new DateOnly(2026, 3, 15),
                TotalAmount = 10.5m,
                TaxAmount = 1m,
                Currency = "EUR",
                Category = "supermercado",
                Confidence = 85.5m,
                CreatedAt = createdAt,
            };
            db.Documents.Add(document);
            await db.SaveChangesAsync();
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

    [Fact]
    public async Task Export_csv_headers_escape_filters_and_empty()
    {
        var r = await _client.GetAsync(new Uri("/documents/export", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal("text/csv", r.Content.Headers.ContentType?.MediaType);
        Assert.Contains(
            "attachment",
            r.Content.Headers.ContentDisposition?.DispositionType ?? string.Empty,
            StringComparison.OrdinalIgnoreCase);

        var raw = await r.Content.ReadAsByteArrayAsync();
        var text = Encoding.UTF8.GetString(raw);
        Assert.StartsWith('\uFEFF' + "Id,", text, StringComparison.Ordinal);

        Assert.Contains("\"Mini Market, \"\"Express\"\"\"", text, StringComparison.Ordinal);

        var rSemi = await _client.GetAsync(new Uri("/documents/export?delimiter=semicolon", UriKind.Relative));
        rSemi.EnsureSuccessStatusCode();
        var textSemi = Encoding.UTF8.GetString(await rSemi.Content.ReadAsByteArrayAsync());
        Assert.StartsWith('\uFEFF' + "Id;", textSemi, StringComparison.Ordinal);

        await using (var db = new ExpenseFlowDbContext(_options))
        {
            await db.Documents.ExecuteDeleteAsync();
        }

        SqliteConnection.ClearAllPools();

        var rEmpty = await _client.GetAsync(new Uri("/documents/export", UriKind.Relative));
        rEmpty.EnsureSuccessStatusCode();
        var textEmpty = Encoding.UTF8.GetString(await rEmpty.Content.ReadAsByteArrayAsync());
        Assert.StartsWith('\uFEFF' + "Id,", textEmpty, StringComparison.Ordinal);
        var lines = textEmpty.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
        Assert.Single(lines);
    }
}
