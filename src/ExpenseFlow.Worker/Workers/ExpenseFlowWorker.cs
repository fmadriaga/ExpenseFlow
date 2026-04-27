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
        var jobId = Guid.NewGuid().ToString("N");
        using var _ = _logger.BeginScope(new Dictionary<string, object> { ["JobId"] = jobId });
        var jobStarted = DateTimeOffset.UtcNow;
        _logger.LogInformation(
            "Processing job started. JobId: {JobId}, StartedAt: {StartedAt:O}",
            jobId,
            jobStarted);

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
            _logger.LogError(
                ex,
                "Scanner failed; ending job cycle. JobId: {JobId}",
                jobId);
            LogJobFinished(jobId, jobStarted, found: 0, processedOk: 0, processedFailed: 0);
            return;
        }

        found = pending.Count;
        _logger.LogInformation(
            "Scanner returned {Count} pending file(s). JobId: {JobId}",
            found,
            jobId);

        foreach (var scan in pending)
        {
            _logger.LogInformation(
                "Candidate file detected. JobId: {JobId}, FileName: {FileName}",
                jobId,
                Path.GetFileName(scan.FullPath));
            stoppingToken.ThrowIfCancellationRequested();
            var success = await ProcessOneFileAsync(jobId, scope, scan, stoppingToken).ConfigureAwait(false);
            if (success)
            {
                processedOk++;
            }
            else
            {
                processedFailed++;
            }
        }

        LogJobFinished(jobId, jobStarted, found, processedOk, processedFailed);
    }

    private void LogJobFinished(
        string jobId,
        DateTimeOffset jobStarted,
        int found,
        int processedOk,
        int processedFailed)
    {
        var finished = DateTimeOffset.UtcNow;
        var duration = finished - jobStarted;
        _logger.LogInformation(
            "Processing job finished. JobId: {JobId}, FinishedAt: {FinishedAt:O}, Duration: {Duration}, FilesFound: {Found}, ProcessedOk: {Ok}, Failed: {Failed}",
            jobId,
            finished,
            duration,
            found,
            processedOk,
            processedFailed);
    }

    private async Task<bool> ProcessOneFileAsync(
        string jobId,
        IServiceScope scope,
        ScanResult scan,
        CancellationToken cancellationToken)
    {
        var db = scope.ServiceProvider.GetRequiredService<ExpenseFlowDbContext>();
        var ocr = scope.ServiceProvider.GetRequiredService<IReceiptOcrProvider>();
        var normalizer = scope.ServiceProvider.GetRequiredService<IReceiptNormalizer>();
        var mover = scope.ServiceProvider.GetRequiredService<IFileMover>();

        var fileStarted = DateTimeOffset.UtcNow;
        var fileName = Path.GetFileName(scan.FullPath);

        _logger.LogInformation(
            "File processing started. JobId: {JobId}, FileName: {FileName}",
            jobId,
            fileName);

        OcrResult ocrResult;

        try
        {
            ocrResult = await ocr
                .AnalyzeReceiptAsync(scan.FullPath, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "OCR failed. JobId: {JobId}, FileName: {FileName}, FullPath: {FullPath}",
                jobId,
                fileName,
                scan.FullPath);
            db.ChangeTracker.Clear();
            await PersistFailureAndMoveToErrorAsync(
                    jobId,
                    db,
                    mover,
                    scan,
                    fileStarted,
                    "ocr_failed",
                    ex,
                    cancellationToken)
                .ConfigureAwait(false);
            return false;
        }

        var normalized = normalizer.Normalize(ocrResult, scan.FullPath, scan.FileHash);
        var finished = DateTimeOffset.UtcNow;

        Document document;
        try
        {
            var pending = await db.Documents
                .Include(d => d.DocumentLines)
                .FirstOrDefaultAsync(
                    d => d.FileHash == scan.FileHash && d.OcrStatus == ReceiptOcrStatuses.Pending,
                    cancellationToken)
                .ConfigureAwait(false);

            if (pending is not null)
            {
                ApplyNormalizedOntoPending(pending, normalized, scan.FullPath, db);
                pending.ProcessingJobs.Add(
                    new ProcessingJob
                    {
                        StartedAt = fileStarted,
                        FinishedAt = finished,
                        Status = ProcessingJobStatuses.Success,
                    });
                document = pending;
            }
            else
            {
                document = normalized;
                document.ProcessingJobs.Add(
                    new ProcessingJob
                    {
                        StartedAt = fileStarted,
                        FinishedAt = finished,
                        Status = ProcessingJobStatuses.Success,
                    });
                db.Documents.Add(document);
            }

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Persistence failed. JobId: {JobId}, FileName: {FileName}, FullPath: {FullPath}",
                jobId,
                fileName,
                scan.FullPath);
            db.ChangeTracker.Clear();
            await PersistFailureAndMoveToErrorAsync(
                    jobId,
                    db,
                    mover,
                    scan,
                    fileStarted,
                    "persistence_failed",
                    ex,
                    cancellationToken)
                .ConfigureAwait(false);
            return false;
        }

        try
        {
            var destinationPath = await mover
                .MoveToProcessedAsync(scan.FullPath, cancellationToken)
                .ConfigureAwait(false);
            _logger.LogInformation(
                "File moved to processed. JobId: {JobId}, FileName: {FileName}, Destination: {Destination}",
                jobId,
                fileName,
                destinationPath);
        }
        catch (Exception moveEx)
        {
            _logger.LogError(
                moveEx,
                "Move to processed failed after DB save. JobId: {JobId}, FileName: {FileName}, FullPath: {FullPath}",
                jobId,
                fileName,
                scan.FullPath);
            await HandleMoveToProcessedFailureAsync(
                    jobId,
                    db,
                    document,
                    moveEx,
                    mover,
                    scan.FullPath,
                    cancellationToken)
                .ConfigureAwait(false);
            return false;
        }

        _logger.LogInformation(
            "File processing finished successfully. JobId: {JobId}, FileName: {FileName}",
            jobId,
            fileName);
        return true;
    }

    private async Task HandleMoveToProcessedFailureAsync(
        string jobId,
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
            var movedTo = await mover.MoveToErrorAsync(sourcePath, cancellationToken).ConfigureAwait(false);
            _logger.LogWarning(
                "File moved to error after processed move failure. JobId: {JobId}, FileName: {FileName}, Reason: {Reason}, Destination: {Destination}",
                jobId,
                Path.GetFileName(sourcePath),
                "processed_move_failed",
                movedTo);
        }
    }

    private async Task PersistFailureAndMoveToErrorAsync(
        string jobId,
        ExpenseFlowDbContext db,
        IFileMover mover,
        ScanResult scan,
        DateTimeOffset fileStarted,
        string failureReason,
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
                "Could not persist failure document. JobId: {JobId}, FileName: {FileName}, FullPath: {FullPath}",
                jobId,
                Path.GetFileName(scan.FullPath),
                scan.FullPath);
        }

        try
        {
            if (File.Exists(scan.FullPath))
            {
                var movedTo = await mover.MoveToErrorAsync(scan.FullPath, cancellationToken).ConfigureAwait(false);
                _logger.LogWarning(
                    "File moved to error. JobId: {JobId}, FileName: {FileName}, FullPath: {FullPath}, Reason: {Reason}, Destination: {Destination}",
                    jobId,
                    Path.GetFileName(scan.FullPath),
                    scan.FullPath,
                    failureReason,
                    movedTo);
            }
        }
        catch (Exception moveEx)
        {
            _logger.LogError(
                moveEx,
                "Could not move file to error folder. JobId: {JobId}, FileName: {FileName}, FullPath: {FullPath}",
                jobId,
                Path.GetFileName(scan.FullPath),
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

    /// <summary>
    /// Actualiza un documento ya existente marcado como Pending (reproceso) con el resultado del OCR.
    /// </summary>
    private static void ApplyNormalizedOntoPending(
        Document pending,
        Document normalized,
        string currentFilePath,
        ExpenseFlowDbContext db)
    {
        pending.FilePath = currentFilePath;
        pending.FileHash = normalized.FileHash;
        pending.MerchantName = normalized.MerchantName;
        pending.TransactionDate = normalized.TransactionDate;
        pending.Currency = normalized.Currency;
        pending.TotalAmount = normalized.TotalAmount;
        pending.TaxAmount = normalized.TaxAmount;
        pending.RawJson = normalized.RawJson;
        pending.Confidence = normalized.Confidence;
        pending.OcrStatus = normalized.OcrStatus;
        pending.ErrorMessage = normalized.ErrorMessage;

        db.DocumentLines.RemoveRange(pending.DocumentLines);
        pending.DocumentLines.Clear();
        foreach (var line in normalized.DocumentLines)
        {
            pending.DocumentLines.Add(
                new DocumentLine
                {
                    Description = line.Description,
                    Quantity = line.Quantity,
                    UnitPrice = line.UnitPrice,
                    Amount = line.Amount,
                    Currency = line.Currency,
                });
        }
    }
}
