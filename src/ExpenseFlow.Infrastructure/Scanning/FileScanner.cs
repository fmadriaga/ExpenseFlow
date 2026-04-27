using ExpenseFlow.Application.Abstractions;
using ExpenseFlow.Application.FileScanning;
using ExpenseFlow.Application.Ocr;
using ExpenseFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace ExpenseFlow.Infrastructure.Scanning;

public sealed class FileScanner : IFileScanner
{
    private const SearchOption InboxSearchDepth = SearchOption.TopDirectoryOnly;
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".pdf",
    };

    private readonly ExpenseFlowDbContext _db;
    private readonly ILogger<FileScanner> _logger;

    public FileScanner(
        ExpenseFlowDbContext db,
        ILogger<FileScanner> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ScanResult>> GetPendingFilesToProcessAsync(
        int familyId,
        string inboxAbsolutePath,
        string processedStorageRoot,
        string errorStorageRoot,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(inboxAbsolutePath))
        {
            throw new ArgumentException("Inbox path is required.", nameof(inboxAbsolutePath));
        }

        if (!Directory.Exists(inboxAbsolutePath))
        {
            _logger.LogWarning("Inbox directory does not exist, skipping scan: {InboxPath}", inboxAbsolutePath);
            return Array.Empty<ScanResult>();
        }

        _logger.LogInformation(
            "Scanning inbox: {InboxPath} (FamilyId: {FamilyId})",
            inboxAbsolutePath,
            familyId);
        var pending = new List<ScanResult>();
        var entries = 0;
        string[] files;
        try
        {
            files = Directory.GetFiles(inboxAbsolutePath, "*", InboxSearchDepth);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to list files in inbox: {InboxPath}",
                inboxAbsolutePath);
            return Array.Empty<ScanResult>();
        }

        foreach (var filePath in files)
        {
            await ProcessOneFileAsync(
                    filePath,
                    familyId,
                    processedStorageRoot,
                    errorStorageRoot,
                    pending,
                    cancellationToken)
                .ConfigureAwait(false);
            entries++;
        }

        _logger.LogInformation(
            "Inbox scan finished. Entries: {FileCount}, pending new: {PendingCount}. FamilyId: {FamilyId}",
            entries,
            pending.Count,
            familyId);
        return pending;
    }

    private async Task ProcessOneFileAsync(
        string filePath,
        int familyId,
        string processedStorageRoot,
        string errorStorageRoot,
        List<ScanResult> pending,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(filePath))
        {
            _logger.LogDebug("File disappeared during scan, skipping: {FilePath}", filePath);
            return;
        }

        if (!IsAllowedExtension(filePath))
        {
            _logger.LogDebug("Ignoring file (not a valid extension): {FilePath}", filePath);
            return;
        }

        _logger.LogDebug("File detected: {FilePath}", filePath);

        long size;
        try
        {
            var info = new FileInfo(filePath);
            if (!info.Exists)
            {
                return;
            }

            size = info.Length;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Inaccessible or invalid file, skipping: {FilePath}", filePath);
            return;
        }

        if (size == 0L)
        {
            _logger.LogInformation("Ignoring empty file: {FilePath}", filePath);
            return;
        }

        string fileHash;
        try
        {
            fileHash = await ComputeFileSha256HexAsync(filePath, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(
                ex,
                "Failed to read file for hashing, skipping: {FilePath}",
                filePath);
            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to hash file, skipping: {FilePath}",
                filePath);
            return;
        }

        _logger.LogDebug("Computed hash for {FilePath}", filePath);
        var successAlready = await _db.Documents
            .AsNoTracking()
            .AnyAsync(
                d =>
                    d.FamilyId == familyId &&
                    d.FileHash == fileHash &&
                    d.OcrStatus == ReceiptOcrStatuses.Success,
                cancellationToken)
            .ConfigureAwait(false);
        if (successAlready)
        {
            _logger.LogWarning(
                "Duplicate file by hash (Success in DB), skipping. FullPath: {FullPath}, FileHash: {FileHashPrefix}…",
                filePath,
                fileHash.Length >= 12
                    ? fileHash[..12]
                    : fileHash);
            return;
        }

        _logger.LogInformation(
            "New candidate to process: {FilePath} Hash: {FileHashPrefix}…",
            filePath,
            fileHash.Length >= 12
                ? fileHash[..12]
                : fileHash);
        pending.Add(
            new ScanResult(
                filePath,
                fileHash,
                IsAlreadyInDatabase: false,
                familyId,
                processedStorageRoot,
                errorStorageRoot));
    }

    private static bool IsAllowedExtension(string filePath)
    {
        var ext = Path.GetExtension(filePath);
        return !string.IsNullOrEmpty(ext) && AllowedExtensions.Contains(ext);
    }

    private static async Task<string> ComputeFileSha256HexAsync(
        string filePath,
        CancellationToken cancellationToken)
    {
        const int bufferSize = 1024 * 8;
        await using var fileStream = new FileStream(
            filePath,
            new FileStreamOptions
            {
                Mode = FileMode.Open,
                Access = FileAccess.Read,
                Share = FileShare.Read,
                BufferSize = bufferSize,
                Options = FileOptions.Asynchronous | FileOptions.SequentialScan,
            });
        var buffer = new byte[bufferSize];
        using var hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        int read;
        while ((read = await fileStream
                       .ReadAsync(
                           buffer.AsMemory(0, bufferSize),
                           cancellationToken)
                       .ConfigureAwait(false)) > 0)
        {
            hasher.AppendData(buffer, 0, read);
        }

        var final = hasher.GetHashAndReset();
        return Convert.ToHexString(final).ToLowerInvariant();
    }
}
