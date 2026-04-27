using ExpenseFlow.Application.Abstractions;
using ExpenseFlow.Application.Ocr;
using ExpenseFlow.Domain.Entities;
using ExpenseFlow.Infrastructure.Data;
using ExpenseFlow.Infrastructure.Scanning;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace ExpenseFlow.IntegrationTests;

public class FileScannerTests
{
    [Fact]
    public async Task Scans_valid_extensions_ignores_txt()
    {
        var inbox = CreateTempInbox();
        var dbPath = Path.GetFullPath(Path.Combine(inbox, "..", "t.db"));
        await using (var _ = new FileStream(dbPath, FileMode.Create)) { }

        var conn = new SqliteConnectionStringBuilder { DataSource = dbPath };

        try
        {
            await File.WriteAllTextAsync(Path.Combine(inbox, "a.txt"), "x");
            await File.WriteAllBytesAsync(Path.Combine(inbox, "b.png"), [1, 2, 3, 4]);
            await File.WriteAllBytesAsync(Path.Combine(inbox, "c.JPG"), [1, 2, 3, 4]);

            using var sp = CreateServiceProvider(conn.ConnectionString, inbox);
            await PrepareDatabaseAsync(sp);
            using var scope = sp.CreateScope();
            var scanner = scope.ServiceProvider.GetRequiredService<IFileScanner>();
            var proc = Path.GetFullPath(Path.Combine(inbox, "..", "p"));
            var err = Path.GetFullPath(Path.Combine(inbox, "..", "e"));
            var r = await scanner.GetPendingFilesToProcessAsync(1, inbox, proc, err);
            Assert.Equal(2, r.Count);
            Assert.DoesNotContain(
                r.Select(s => s.FullPath),
                p => p.EndsWith("a.txt", StringComparison.Ordinal));
            Assert.Contains(
                r.Select(s => s.FullPath),
                p => p.EndsWith("b.png", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(
                r.Select(s => s.FullPath),
                p => p.EndsWith("c.JPG", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            await CleanupDbFileAsync(dbPath);
        }
    }

    [Fact]
    public async Task Skips_zero_byte_files()
    {
        var inbox = CreateTempInbox();
        var dbPath = Path.GetFullPath(Path.Combine(inbox, "..", "e.db"));
        await using (var _ = new FileStream(dbPath, FileMode.Create)) { }
        var conn = new SqliteConnectionStringBuilder { DataSource = dbPath };

        try
        {
            var emptyJpg = Path.Combine(inbox, "empty.jpg");
            await File.WriteAllTextAsync(emptyJpg, string.Empty, Encoding.Latin1);
            var png = Path.Combine(inbox, "a.png");
            await File.WriteAllBytesAsync(png, [7]);

            using var sp = CreateServiceProvider(conn.ConnectionString, inbox);
            await PrepareDatabaseAsync(sp);
            using var scope = sp.CreateScope();
            var scanner = scope.ServiceProvider.GetRequiredService<IFileScanner>();
            var proc = Path.GetFullPath(Path.Combine(inbox, "..", "p"));
            var err = Path.GetFullPath(Path.Combine(inbox, "..", "e"));
            var r = await scanner.GetPendingFilesToProcessAsync(1, inbox, proc, err);
            Assert.Single(r);
            Assert.Equal(png, r[0].FullPath, StringComparer.OrdinalIgnoreCase);
        }
        finally
        {
            await CleanupDbFileAsync(dbPath);
        }
    }

    [Fact]
    public async Task Skips_file_when_hash_already_in_database()
    {
        var inbox = CreateTempInbox();
        var dbPath = Path.GetFullPath(Path.Combine(inbox, "..", "d.db"));
        await using (var _ = new FileStream(dbPath, FileMode.Create)) { }
        var conn = new SqliteConnectionStringBuilder { DataSource = dbPath };
        const string content = "dup-content";
        var filePath = Path.Combine(inbox, "t.jpg");
        var bytes = Encoding.UTF8.GetBytes(content);
        await File.WriteAllBytesAsync(filePath, bytes);
        var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

        try
        {
            using var sp = CreateServiceProvider(conn.ConnectionString, inbox);
            await PrepareDatabaseAsync(sp);
            {
                using var pre = sp.CreateScope();
                var db = pre.ServiceProvider.GetRequiredService<ExpenseFlowDbContext>();
                db.Documents.Add(
                    new Document
                    {
                        FamilyId = 1,
                        FilePath = "earlier",
                        FileHash = hash,
                        OcrStatus = ReceiptOcrStatuses.Success,
                        CreatedAt = DateTimeOffset.UtcNow,
                    });
                await db.SaveChangesAsync();
            }

            using var scope = sp.CreateScope();
            var scanner = scope.ServiceProvider.GetRequiredService<IFileScanner>();
            var proc = Path.GetFullPath(Path.Combine(inbox, "..", "p"));
            var err = Path.GetFullPath(Path.Combine(inbox, "..", "e"));
            var r = await scanner.GetPendingFilesToProcessAsync(1, inbox, proc, err);
            Assert.Empty(r);
        }
        finally
        {
            await CleanupDbFileAsync(dbPath);
        }
    }

    private static string CreateTempInbox() =>
        Directory.CreateDirectory(
            Path.Combine(
                Path.GetTempPath(),
                "expenseflow_inbox_test_" + Guid.NewGuid().ToString("N"))).FullName;

    private static async Task PrepareDatabaseAsync(ServiceProvider sp)
    {
        using var init = sp.CreateScope();
        var db = init.ServiceProvider.GetRequiredService<ExpenseFlowDbContext>();
        await db.Database.MigrateAsync();
    }

    private static ServiceProvider CreateServiceProvider(
        string connectionString,
        string absoluteInbox)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ExpenseFlowDbContext>(o => o.UseSqlite(connectionString));
        services.AddScoped<IFileScanner, FileScanner>();
        return services.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateScopes = true,
            });
    }

    private static async Task CleanupDbFileAsync(string dbPath)
    {
        await Task.Yield();
        SqliteConnection.ClearAllPools();
        if (File.Exists(dbPath))
        {
            File.Delete(dbPath);
        }
    }
}
