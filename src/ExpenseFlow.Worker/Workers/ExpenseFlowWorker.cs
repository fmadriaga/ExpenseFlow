using ExpenseFlow.Application.Abstractions;
using ExpenseFlow.Application.FileScanning;
using ExpenseFlow.Application.Options;
using ExpenseFlow.Application.Ocr;
using ExpenseFlow.Application.Processing;
using ExpenseFlow.Domain.Entities;
using ExpenseFlow.Infrastructure.Configuration;
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
        var db = scope.ServiceProvider.GetRequiredService<ExpenseFlowDbContext>();
        var hostEnv = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
        var scanner = scope.ServiceProvider.GetRequiredService<IFileScanner>();

        var families = await db.Families
            .AsNoTracking()
            .OrderBy(f => f.Id)
            .ToListAsync(stoppingToken)
            .ConfigureAwait(false);
        if (families.Count == 0)
        {
            _logger.LogWarning("No families in database; nothing to scan. JobId: {JobId}", jobId);
            LogJobFinished(jobId, jobStarted, 0, 0, 0);
            return;
        }

        var pending = new List<ScanResult>();
        try
        {
            foreach (var family in families)
            {
                string inboxAbs;
                string procAbs;
                string errAbs;
                try
                {
                    inboxAbs = ContentRootPathResolver.Resolve(hostEnv.ContentRootPath, family.InboxPath);
                    procAbs = ContentRootPathResolver.Resolve(hostEnv.ContentRootPath, family.ProcessedPath);
                    errAbs = ContentRootPathResolver.Resolve(hostEnv.ContentRootPath, family.ErrorPath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Invalid storage path for family. JobId: {JobId}, FamilyId: {FamilyId}",
                        jobId,
                        family.Id);
                    continue;
                }

                var part = await scanner
                    .GetPendingFilesToProcessAsync(
                        family.Id,
                        inboxAbs,
                        procAbs,
                        errAbs,
                        stoppingToken)
                    .ConfigureAwait(false);
                pending.AddRange(part);
            }
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
        WorkerCycleMetrics.FilesFound.Add(found);
        WorkerCycleMetrics.FilesProcessedOk.Add(processedOk);
        WorkerCycleMetrics.FilesProcessedFailed.Add(processedFailed);
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
        var categorizer = scope.ServiceProvider.GetRequiredService<IExpenseCategorizer>();
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
                    categorizer,
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
            var existing = await db.Documents
                .Include(d => d.DocumentLines)
                .FirstOrDefaultAsync(
                    d =>
                        d.FamilyId == scan.FamilyId &&
                        d.FileHash == scan.FileHash,
                    cancellationToken)
                .ConfigureAwait(false);

            if (existing is not null)
            {
                ApplyNormalizedOntoPending(existing, normalized, scan.FullPath, db);
                existing.ProcessingJobs.Add(
                    new ProcessingJob
                    {
                        StartedAt = fileStarted,
                        FinishedAt = finished,
                        Status = ProcessingJobStatuses.Success,
                    });
                document = existing;
            }
            else
            {
                document = normalized;
                document.FamilyId = scan.FamilyId;
                document.ProcessingJobs.Add(
                    new ProcessingJob
                    {
                        StartedAt = fileStarted,
                        FinishedAt = finished,
                        Status = ProcessingJobStatuses.Success,
                    });
                db.Documents.Add(document);
            }

            document.Category = categorizer.Categorize(document);

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException dbEx) when (IsSqliteUniqueConstraintOnDocumentsFamilyHash(dbEx))
        {
            db.ChangeTracker.Clear();
            _logger.LogWarning(
                "Duplicate file hash (unique index); document already in database. JobId: {JobId}, FullPath: {FullPath}, FileHash: {FileHash}",
                jobId,
                scan.FullPath,
                scan.FileHash);
            return await TryMoveDuplicateInboxToProcessedOrFailAsync(
                jobId,
                mover,
                scan,
                cancellationToken)
                .ConfigureAwait(false);
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
                    categorizer,
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
                .MoveToProcessedAsync(scan.FullPath, scan.ProcessedStorageRoot, cancellationToken)
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
                    scan.ErrorStorageRoot,
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
        string errorStorageRoot,
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
            var movedTo = await mover
                .MoveToErrorAsync(sourcePath, errorStorageRoot, cancellationToken)
                .ConfigureAwait(false);
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
        IExpenseCategorizer categorizer,
        ScanResult scan,
        DateTimeOffset fileStarted,
        string failureReason,
        Exception ex,
        CancellationToken cancellationToken)
    {
        var duplicateOnFailureSave = false;
        try
        {
            var errorDoc = new Document
            {
                FamilyId = scan.FamilyId,
                FilePath = scan.FullPath,
                FileHash = scan.FileHash,
                CreatedAt = fileStarted,
                OcrStatus = ReceiptOcrStatuses.Failed,
                ErrorMessage = TruncateErrorMessage(ex),
                Confidence = 0m,
            };
            errorDoc.Category = categorizer.Categorize(errorDoc);
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
        catch (DbUpdateException dbEx) when (IsSqliteUniqueConstraintOnDocumentsFamilyHash(dbEx))
        {
            db.ChangeTracker.Clear();
            _logger.LogWarning(
                "Duplicate file hash (unique index) while persisting failure document; record already exists. JobId: {JobId}, FullPath: {FullPath}, FileHash: {FileHash}",
                jobId,
                scan.FullPath,
                scan.FileHash);
            duplicateOnFailureSave = true;
            if (File.Exists(scan.FullPath))
            {
                try
                {
                    var dest = await mover
                        .MoveToProcessedAsync(scan.FullPath, scan.ProcessedStorageRoot, cancellationToken)
                        .ConfigureAwait(false);
                    _logger.LogWarning(
                        "Duplicate hash after failure: file moved to processed. JobId: {JobId}, FullPath: {FullPath}, FileHash: {FileHash}, Destination: {Destination}",
                        jobId,
                        scan.FullPath,
                        scan.FileHash,
                        dest);
                }
                catch (Exception moveDup)
                {
                    _logger.LogError(
                        moveDup,
                        "Could not move duplicate failure file to processed. JobId: {JobId}, FileName: {FileName}, FullPath: {FullPath}, FileHash: {FileHash}",
                        jobId,
                        Path.GetFileName(scan.FullPath),
                        scan.FullPath,
                        scan.FileHash);
                }
            }
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

        if (duplicateOnFailureSave)
        {
            return;
        }

        try
        {
            if (File.Exists(scan.FullPath))
            {
                var movedTo = await mover
                    .MoveToErrorAsync(scan.FullPath, scan.ErrorStorageRoot, cancellationToken)
                    .ConfigureAwait(false);
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

    /// <summary>
    /// Tras violación de índice único en FileHash, mueve el fichero del inbox a processed (duplicado lógico v/s BD).
    /// </summary>
    private async Task<bool> TryMoveDuplicateInboxToProcessedOrFailAsync(
        string jobId,
        IFileMover mover,
        ScanResult scan,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(scan.FullPath))
        {
            _logger.LogWarning(
                "Duplicate hash: inbox file not found (already moved?). JobId: {JobId}, FullPath: {FullPath}, FileHash: {FileHash}",
                jobId,
                scan.FullPath,
                scan.FileHash);
            return true;
        }

        try
        {
            var dest = await mover
                .MoveToProcessedAsync(scan.FullPath, scan.ProcessedStorageRoot, cancellationToken)
                .ConfigureAwait(false);
            _logger.LogWarning(
                "Duplicate hash: file moved to processed. JobId: {JobId}, FullPath: {FullPath}, FileHash: {FileHash}, Destination: {Destination}",
                jobId,
                scan.FullPath,
                scan.FileHash,
                dest);
            return true;
        }
        catch (Exception moveEx)
        {
            _logger.LogError(
                moveEx,
                "Could not move duplicate to processed. JobId: {JobId}, FileName: {FileName}, FullPath: {FullPath}, FileHash: {FileHash}",
                jobId,
                Path.GetFileName(scan.FullPath),
                scan.FullPath,
                scan.FileHash);
            return false;
        }
    }

    private static bool IsSqliteUniqueConstraintOnDocumentsFamilyHash(DbUpdateException ex)
    {
        for (var e = (Exception?)ex; e is not null; e = e.InnerException)
        {
            if (!e.Message.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (e.Message.Contains("Documents.FamilyId", StringComparison.Ordinal) &&
                e.Message.Contains("Documents.FileHash", StringComparison.Ordinal))
            {
                return true;
            }

            // Migraciones antiguas u otros índices
            if (e.Message.Contains("FileHash", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
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
