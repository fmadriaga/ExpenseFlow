using ExpenseFlow.Application.Abstractions;
using ExpenseFlow.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ExpenseFlow.Infrastructure.Storage;

public sealed class FileMover : IFileMover
{
    private const string ProcessedBucket = "processed";
    private const string ErrorBucket = "error";

    private readonly IOptions<StorageOptions> _options;
    private readonly ILogger<FileMover> _logger;

    public FileMover(IOptions<StorageOptions> options, ILogger<FileMover> logger)
    {
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<string> MoveToProcessedAsync(string sourcePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(
            MoveCore(
                sourcePath,
                _options.Value.Processed,
                ProcessedBucket,
                cancellationToken));
    }

    /// <inheritdoc />
    public Task<string> MoveToErrorAsync(string sourcePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(
            MoveCore(
                sourcePath,
                _options.Value.Error,
                ErrorBucket,
                cancellationToken));
    }

    private string MoveCore(
        string sourcePath,
        string storageRoot,
        string bucketLabel,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            throw new ArgumentException("Source path is required.", nameof(sourcePath));
        }

        var fullSource = Path.GetFullPath(sourcePath);
        if (!File.Exists(fullSource))
        {
            throw new FileNotFoundException("Source file does not exist.", fullSource);
        }

        if (string.IsNullOrWhiteSpace(storageRoot))
        {
            throw new InvalidOperationException(
                $"Storage root for '{bucketLabel}' is not configured.");
        }

        var fullRoot = Path.GetFullPath(storageRoot);
        var utcNow = DateTime.UtcNow;
        var segment = Path.Combine(utcNow.ToString("yyyy"), utcNow.ToString("MM"));
        var destDir = Path.GetFullPath(Path.Combine(fullRoot, segment));

        try
        {
            Directory.CreateDirectory(destDir);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to create destination directory for {Bucket}. Path: {DestinationDirectory}",
                bucketLabel,
                destDir);
            throw;
        }

        var fileName = Path.GetFileName(fullSource);
        var preferredDest = Path.GetFullPath(Path.Combine(destDir, fileName));
        var finalDest = EnsureUniqueDestinationPath(preferredDest);
        var destinationFileName = Path.GetFileName(finalDest);

        if (!string.Equals(preferredDest, finalDest, StringComparison.Ordinal))
        {
            _logger.LogInformation(
                "Duplicate destination name avoided (not overwriting). Bucket: {Bucket}, SourceFileName: {SourceFileName}, DestinationFileName: {DestinationFileName}",
                bucketLabel,
                fileName,
                destinationFileName);
        }

        try
        {
            File.Move(fullSource, finalDest);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to move file to {Bucket}. SourceFileName: {SourceFileName}, DestinationFileName: {DestinationFileName}",
                bucketLabel,
                fileName,
                destinationFileName);
            throw;
        }

        _logger.LogInformation(
            "Moved file to {Bucket}. SourceFileName: {SourceFileName}, DestinationFileName: {DestinationFileName}",
            bucketLabel,
            fileName,
            destinationFileName);

        return finalDest;
    }

    private static string EnsureUniqueDestinationPath(string preferredPath)
    {
        if (!File.Exists(preferredPath))
        {
            return preferredPath;
        }

        var directory = Path.GetDirectoryName(preferredPath)
            ?? throw new InvalidOperationException("Could not resolve destination directory.");
        var extension = Path.GetExtension(preferredPath);
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(preferredPath);

        string candidate;
        do
        {
            candidate = Path.Combine(
                directory,
                $"{nameWithoutExtension}_{Guid.NewGuid():N}{extension}");
        }
        while (File.Exists(candidate));

        return candidate;
    }
}
