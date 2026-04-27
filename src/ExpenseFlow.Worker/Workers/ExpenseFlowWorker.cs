using ExpenseFlow.Application.Abstractions;
using ExpenseFlow.Application.FileScanning;
using ExpenseFlow.Application.Options;
using ExpenseFlow.Application.Ocr;
using ExpenseFlow.Application.Processing;
using ExpenseFlow.Domain.Entities;
using ExpenseFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ExpenseFlow.Worker;

public sealed class ExpenseFlowWorker : BackgroundService
{
    private const int MinimumIntervalSeconds = 1;
    private const int MaxErrorMessageLength = 4000;

    private readonly ILogger<ExpenseFlowWorker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<WorkerOptions> _workerOptions;
    private readonly SemaphoreSlim _cycleGate = new(1, 1);

    public ExpenseFlowWorker(
        ILogger<ExpenseFlowWorker> logger,
        IServiceScopeFactory scopeFactory,
        IOptions<WorkerOptions> workerOptions)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _workerOptions = workerOptions;
    }

    public override void Dispose()
    {
        _cycleGate.Dispose();
        base.Dispose();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalSeconds = Math.Max(MinimumIntervalSeconds, _workerOptions.Value.IntervalSeconds);
        _logger.LogInformation(
            "ExpenseFlow worker started. Interval: {IntervalSeconds}s",
            intervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            var entered = await _cycleGate
                .WaitAsync(TimeSpan.Zero, stoppingToken)
                .ConfigureAwait(false);
            if (!entered)
            {
                _logger.LogWarning(
                    "Previous processing cycle still running; skipping overlapping tick.");
                await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken)
                    .ConfigureAwait(false);
                continue;
            }

            try
            {
                await RunJobCycleAsync(stoppingToken).ConfigureAwait(false);
            }
            finally
            {
                _cycleGate.Release();
            }

            await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken)
                .ConfigureAwait(false);
        }
    }

    private async Task RunJobCycleAsync(CancellationToken stoppingToken)
    {
        var jobStarted = DateTimeOffset.UtcNow;
        _logger.LogInformation("Processing job started at {StartedAt:O}", jobStarted);

        var found = 0;
        var processedOk = 0;
        var processedFailed = 0;

        using var scope = _scopeFactory.CreateScope();
        var scanner = scope.ServiceProvider.GetRequiredService<IFileScanner>();

        IReadOnlyList<ScanResult> pending;
        try
        {
            pending = await scanner
                .GetPendingFilesToProcessAsync(stoppingToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Scanner failed; ending job cycle.");
            LogJobFinished(jobStarted, found: 0, processedOk: 0, processedFailed: 0);
            return;
        }

        found = pending.Count;
        _logger.LogInformation("Scanner returned {Count} pending file(s).", found);

        foreach (var scan in pending)
        {
            stoppingToken.ThrowIfCancellationRequested();
            var success = await ProcessOneFileAsync(scope, scan, stoppingToken).ConfigureAwait(false);
            if (success)
            {
                processedOk++;
            }
            else
            {
                processedFailed++;
            }
        }

        LogJobFinished(jobStarted, found, processedOk, processedFailed);
    }

    private void LogJobFinished(
        DateTimeOffset jobStarted,
        int found,
        int processedOk,
        int processedFailed)
    {
        var finished = DateTimeOffset.UtcNow;
        var duration = finished - jobStarted;
        _logger.LogInformation(
            "Processing job finished at {FinishedAt:O}. Duration: {Duration}. Files found: {Found}, processed OK: {Ok}, failed: {Failed}",
            finished,
            duration,
            found,
            processedOk,
            processedFailed);
    }

    private async Task<bool> ProcessOneFileAsync(
        IServiceScope scope,
        ScanResult scan,
        CancellationToken cancellationToken)
    {
        var db = scope.ServiceProvider.GetRequiredService<ExpenseFlowDbContext>();
        var ocr = scope.ServiceProvider.GetRequiredService<IReceiptOcrProvider>();
        var normalizer = scope.ServiceProvider.GetRequiredService<IReceiptNormalizer>();
        var mover = scope.ServiceProvider.GetRequiredService<IFileMover>();

        var fileStarted = DateTimeOffset.UtcNow;
        Document document;

        try
        {
            var ocrResult = await ocr
                .AnalyzeReceiptAsync(scan.FullPath, cancellationToken)
                .ConfigureAwait(false);
            document = normalizer.Normalize(ocrResult, scan.FullPath, scan.FileHash);

            var finished = DateTimeOffset.UtcNow;
            document.ProcessingJobs.Add(
                new ProcessingJob
                {
                    StartedAt = fileStarted,
                    FinishedAt = finished,
                    Status = ProcessingJobStatuses.Success,
                });

            db.Documents.Add(document);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Processing failed before or during persist for {Path}", scan.FullPath);
            db.ChangeTracker.Clear();
            await PersistFailureAndMoveToErrorAsync(
                    db,
                    mover,
                    scan,
                    fileStarted,
                    ex,
                    cancellationToken)
                .ConfigureAwait(false);
            return false;
        }

        try
        {
            await mover
                .MoveToProcessedAsync(scan.FullPath, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception moveEx)
        {
            _logger.LogError(
                moveEx,
                "Move to processed failed after DB save for {Path}",
                scan.FullPath);
            await HandleMoveToProcessedFailureAsync(
                    db,
                    document,
                    moveEx,
                    mover,
                    scan.FullPath,
                    cancellationToken)
                .ConfigureAwait(false);
            return false;
        }

        return true;
    }

    private static async Task HandleMoveToProcessedFailureAsync(
        ExpenseFlowDbContext db,
        Document document,
        Exception moveEx,
        IFileMover mover,
        string sourcePath,
        CancellationToken cancellationToken)
    {
        document.OcrStatus = ReceiptOcrStatuses.Failed;
        document.ErrorMessage = TruncateErrorMessage(
            $"Move to processed failed: {moveEx.Message}");
        foreach (var job in document.ProcessingJobs)
        {
            job.Status = ProcessingJobStatuses.Failed;
            job.FinishedAt = DateTimeOffset.UtcNow;
            job.ErrorMessage = TruncateErrorMessage(moveEx.Message);
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (File.Exists(sourcePath))
        {
            await mover.MoveToErrorAsync(sourcePath, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task PersistFailureAndMoveToErrorAsync(
        ExpenseFlowDbContext db,
        IFileMover mover,
        ScanResult scan,
        DateTimeOffset fileStarted,
        Exception ex,
        CancellationToken cancellationToken)
    {
        try
        {
            var errorDoc = new Document
            {
                FilePath = scan.FullPath,
                FileHash = scan.FileHash,
                CreatedAt = fileStarted,
                OcrStatus = ReceiptOcrStatuses.Failed,
                ErrorMessage = TruncateErrorMessage(ex),
                Confidence = 0m,
            };
            errorDoc.ProcessingJobs.Add(
                new ProcessingJob
                {
                    StartedAt = fileStarted,
                    FinishedAt = DateTimeOffset.UtcNow,
                    Status = ProcessingJobStatuses.Failed,
                    ErrorMessage = TruncateErrorMessage(ex.Message),
                });
            db.Documents.Add(errorDoc);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception saveEx)
        {
            _logger.LogError(
                saveEx,
                "Could not persist failure document for {Path}",
                scan.FullPath);
        }

        try
        {
            if (File.Exists(scan.FullPath))
            {
                await mover.MoveToErrorAsync(scan.FullPath, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception moveEx)
        {
            _logger.LogError(
                moveEx,
                "Could not move file to error folder for {Path}",
                scan.FullPath);
        }
    }

    private static string TruncateErrorMessage(Exception ex) =>
        TruncateErrorMessage($"{ex.GetType().Name}: {ex.Message}");

    private static string TruncateErrorMessage(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return string.Empty;
        }

        return message.Length <= MaxErrorMessageLength
            ? message
            : message[..MaxErrorMessageLength];
    }
}
