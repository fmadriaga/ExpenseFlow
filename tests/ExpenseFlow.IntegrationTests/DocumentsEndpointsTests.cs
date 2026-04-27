using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using ExpenseFlow.Application.DTOs;
using ExpenseFlow.Domain.Entities;
using ExpenseFlow.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ExpenseFlow.IntegrationTests;

[CollectionDefinition("DocumentsApi serial", DisableParallelization = true)]
public sealed class DocumentsApiSerialCollection;

public sealed class DocumentsApiFactory : WebApplicationFactory<Program>
{
    public readonly string DbFile = Path.GetFullPath(
        Path.Combine(
            Path.GetTempPath(),
            $"expenseflow_api_docs_test_{Guid.NewGuid():N}.db"));

    /// <summary>
    /// Misma forma que <see cref="Infrastructure.DependencyInjection"/>: Data Source=...
    /// para alinear con <c>AddPersistence</c> cuando la cadena es "tipo SQLite" completo.
    /// </summary>
    public string SqliteConnectionString { get; }

    private readonly string _tempRoot = Path.Combine(
        Path.GetTempPath(),
        $"expenseflow_api_storage_{Guid.NewGuid():N}");

    public string TempRoot => _tempRoot;

    public int SeededDocumentId { get; set; }

    public DocumentsApiFactory()
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

        // "Testing" evita Migrate en Program (véase Program.cs) y deja el esquema al test.
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

[Collection("DocumentsApi serial")]
public class DocumentsEndpointsTests : IClassFixture<DocumentsApiFactory>, IAsyncLifetime
{
    private readonly DocumentsApiFactory _factory;
    private HttpClient _client = null!;

    public DocumentsEndpointsTests(DocumentsApiFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        // Esquema y seed con un contexto local (mismo archivo) antes de levantar el host.
        // Así evitamos contención de Migrate/EF9 y el host lee una BD ya creada.
        var options = new DbContextOptionsBuilder<ExpenseFlowDbContext>()
            .UseSqlite(_factory.SqliteConnectionString)
            .Options;
        await using (var db = new ExpenseFlowDbContext(options))
        {
            await db.Database.EnsureDeletedAsync();
            await db.Database.MigrateAsync();

            var inbox = Path.Combine(_factory.TempRoot, "inbox");
            var processed = Path.Combine(_factory.TempRoot, "processed");
            var error = Path.Combine(_factory.TempRoot, "error");
            var fam = await db.Families.FirstAsync(f => f.Id == 1);
            fam.InboxPath = inbox;
            fam.ProcessedPath = processed;
            fam.ErrorPath = error;
            await db.SaveChangesAsync();

            var createdAt = new DateTimeOffset(2026, 4, 1, 12, 0, 0, TimeSpan.Zero);
            var document = new Document
            {
                FamilyId = 1,
                FilePath = "/inbox/ticket.png",
                FileHash = "a1b2c3d4e5f67890123456789012abcd",
                RawJson = """{"test":true}""",
                OcrStatus = "Success",
                ErrorMessage = null,
                MerchantName = "Café Central",
                TransactionDate = new DateOnly(2026, 3, 15),
                TotalAmount = 10.5m,
                Currency = "EUR",
                Confidence = 85.5m,
                CreatedAt = createdAt,
            };
            document.DocumentLines.Add(
                new DocumentLine
                {
                    Description = "Café",
                    Quantity = 1,
                    UnitPrice = 10.5m,
                    Amount = 10.5m,
                    Currency = "EUR",
                });
            db.Documents.Add(document);
            await db.SaveChangesAsync();
            _factory.SeededDocumentId = document.Id;
        }

        SqliteConnection.ClearAllPools();
        _client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
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
    public async Task Documents_list_detail_and_404()
    {
        // Mismo almacenamiento que el host (un solo [Fact] evita carreras con el TestServer).
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ExpenseFlowDbContext>();
            Assert.Equal(1, await db.Documents.CountAsync());
        }

        var listResponse = await _client.GetAsync(new Uri("/documents", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var list = await listResponse.Content.ReadFromJsonAsync<DocumentsListResponseDto>();
        Assert.NotNull(list);
        Assert.NotNull(list!.Items);
        Assert.NotEmpty(list.Items);

        var id = _factory.SeededDocumentId;
        var detailResponse = await _client.GetAsync(new Uri($"/documents/{id}", UriKind.Relative));
        detailResponse.EnsureSuccessStatusCode();
        var detail = await detailResponse.Content.ReadFromJsonAsync<DocumentDetailDto>();
        Assert.NotNull(detail);
        Assert.Equal(id, detail!.Id);
        Assert.Equal("Café Central", detail.MerchantName);
        Assert.NotNull(detail.RawJson);
        Assert.NotEmpty(detail.Lines);

        var notFound = await _client.GetAsync(new Uri("/documents/9999999", UriKind.Relative));
        Assert.Equal(HttpStatusCode.NotFound, notFound.StatusCode);
    }
}
