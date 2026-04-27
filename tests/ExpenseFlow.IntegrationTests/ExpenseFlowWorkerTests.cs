using ExpenseFlow.Application.Abstractions;
using ExpenseFlow.Application.FileScanning;
using ExpenseFlow.Application.Ocr;
using ExpenseFlow.Application.Options;
using ExpenseFlow.Application.Services;
using ExpenseFlow.Infrastructure.Data;
using ExpenseFlow.Infrastructure.Storage;
using ExpenseFlow.Worker;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace ExpenseFlow.IntegrationTests;

public class ExpenseFlowWorkerTests
{
    [Fact]
    public async Task ExpenseFlowWorker_host_starts_and_completes_cycle_without_throwing()
    {
        var dbPath = Path.Combine(
            Path.GetTempPath(),
            "expenseflow_worker_smoke_" + Guid.NewGuid().ToString("N") + ".db");
        var root = Path.Combine(
            Path.GetTempPath(),
            "expenseflow_worker_root_" + Guid.NewGuid().ToString("N"));

        try
        {
            var inbox = Path.Combine(root, "inbox");
            Directory.CreateDirectory(inbox);

            var builder = Host.CreateApplicationBuilder();
            builder.Configuration["ConnectionStrings:ExpenseFlow"] = dbPath;
            builder.Services.Configure<WorkerOptions>(o => o.IntervalSeconds = 1);
            builder.Services.Configure<StorageOptions>(o =>
            {
                o.Inbox = inbox;
                o.Processed = Path.Combine(root, "processed");
                o.Error = Path.Combine(root, "error");
            });
            builder.Services.AddLogging();
            builder.Services.AddDbContext<ExpenseFlowDbContext>(o => o.UseSqlite(
                new SqliteConnectionStringBuilder { DataSource = dbPath }.ConnectionString));
            builder.Services.AddSingleton<IFileScanner, EmptyFileScannerStub>();
            builder.Services.AddSingleton<IReceiptOcrProvider, StubOcrProvider>();
            builder.Services.AddSingleton<IReceiptNormalizer, ReceiptNormalizer>();
            builder.Services.AddScoped<IFileMover, FileMover>();
            builder.Services.AddHostedService<ExpenseFlowWorker>();

            using var host = builder.Build();
            using (var scope = host.Services.CreateScope())
            {
                await scope.ServiceProvider
                    .GetRequiredService<ExpenseFlowDbContext>()
                    .Database
                    .MigrateAsync();
            }

            await host.StartAsync();
            await Task.Delay(2500);
            await host.StopAsync();
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
            }

            if (Directory.Exists(root))
            {
                try
                {
                    Directory.Delete(root, recursive: true);
                }
                catch
                {
                    // best-effort
                }
            }
        }
    }

    private sealed class EmptyFileScannerStub : IFileScanner
    {
        public Task<IReadOnlyList<ScanResult>> GetPendingFilesToProcessAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ScanResult>>(Array.Empty<ScanResult>());
    }

    private sealed class StubOcrProvider : IReceiptOcrProvider
    {
        public Task<OcrResult> AnalyzeReceiptAsync(
            string filePath,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                new OcrResult(
                    "Stub",
                    null,
                    1m,
                    null,
                    "{}",
                    Array.Empty<OcrLineItem>(),
                    "EUR"));
    }
}
