using System.Security.Cryptography;
using ExpenseFlow.Application.Abstractions;
using ExpenseFlow.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ExpenseFlow.Infrastructure.Storage;

public sealed class FileRestorer : IFileRestorer
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".pdf",
    };

    private const int HashBufferSize = 1024 * 8;

    private readonly IOptions<StorageOptions> _options;
    private readonly ILogger<FileRestorer> _logger;

    public FileRestorer(IOptions<StorageOptions> options, ILogger<FileRestorer> logger)
    {
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string?> FindSourcePathInErrorTreeAsync(
        string fileHashHex,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileHashHex))
        {
            return null;
        }

        var expected = NormalizeHash(fileHashHex);
        if (expected is null)
        {
            return null;
        }

        var errorRoot = _options.Value.Error;
        if (string.IsNullOrWhiteSpace(errorRoot))
        {
            _logger.LogWarning("Error root not configured; cannot search for file to restore.");
            return null;
        }

        var fullErrorRoot = Path.GetFullPath(errorRoot);
        if (!Directory.Exists(fullErrorRoot))
        {
            _logger.LogWarning(
                "Reprocess: error directory does not exist, cannot find file. Path: {ErrorRoot}",
                fullErrorRoot);
            return null;
        }

        string[] files;
        try
        {
            files = Directory.GetFiles(fullErrorRoot, "*", SearchOption.AllDirectories);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Reprocess: failed to list error tree. Path: {ErrorRoot}",
                fullErrorRoot);
            return null;
        }

        foreach (var filePath in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsAllowedExtension(filePath))
            {
                continue;
            }

            try
            {
                var info = new FileInfo(filePath);
                if (!info.Exists || info.Length == 0)
                {
                    continue;
                }
            }
            catch (IOException)
            {
                continue;
            }

            string hash;
            try
            {
                hash = await ComputeFileSha256HexAsync(filePath, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                continue;
            }

            if (string.Equals(NormalizeHash(hash), expected, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation(
                    "Reprocess: found file in error tree matching hash. Path: {FilePath}",
                    filePath);
                return filePath;
            }
        }

        _logger.LogWarning(
            "Reprocess: no file under error tree matched FileHash prefix {HashPrefix}…",
            expected.Length >= 12
                ? expected[..12]
                : expected);
        return null;
    }

    /// <inheritdoc />
    public Task<string?> RestoreToInboxAsync(string sourcePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            throw new ArgumentException("Source path is required.", nameof(sourcePath));
        }

        var fullSource = Path.GetFullPath(sourcePath);
        if (!File.Exists(fullSource))
        {
            _logger.LogWarning(
                "Reprocess: source file does not exist, skipping move to inbox. Path: {SourcePath}",
                fullSource);
            return Task.FromResult<string?>(null);
        }

        var inboxRoot = _options.Value.Inbox;
        if (string.IsNullOrWhiteSpace(inboxRoot))
        {
            throw new InvalidOperationException("Storage:Inbox is not configured.");
        }

        var fullInboxRoot = Path.GetFullPath(inboxRoot);
        try
        {
            Directory.CreateDirectory(fullInboxRoot);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Reprocess: failed to create inbox directory. Path: {InboxPath}",
                fullInboxRoot);
            throw;
        }

        var fileName = Path.GetFileName(fullSource);
        var preferredDest = Path.GetFullPath(Path.Combine(fullInboxRoot, fileName));
        var finalDest = EnsureUniqueDestinationPath(preferredDest);

        if (!string.Equals(preferredDest, finalDest, StringComparison.Ordinal))
        {
            _logger.LogInformation(
                "Reprocess: duplicate inbox name avoided (not overwriting). DestinationFileName: {DestinationFileName}",
                Path.GetFileName(finalDest));
        }

        try
        {
            File.Move(fullSource, finalDest);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Reprocess: failed to move file from error to inbox. Source: {Source}, Destination: {Destination}",
                fullSource,
                finalDest);
            throw;
        }

        _logger.LogInformation(
            "Reprocess: moved file to inbox. Destination: {Destination}",
            finalDest);

        return Task.FromResult<string?>(finalDest);
    }

    private static string? NormalizeHash(string hex)
    {
        var s = hex.Trim();
        return string.IsNullOrEmpty(s)
            ? null
            : s.ToLowerInvariant();
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
        await using var fileStream = new FileStream(
            filePath,
            new FileStreamOptions
            {
                Mode = FileMode.Open,
                Access = FileAccess.Read,
                Share = FileShare.Read,
                BufferSize = HashBufferSize,
                Options = FileOptions.Asynchronous | FileOptions.SequentialScan,
            });
        var buffer = new byte[HashBufferSize];
        using var hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        int read;
        while ((read = await fileStream
                       .ReadAsync(
                           buffer.AsMemory(0, HashBufferSize),
                           cancellationToken)
                       .ConfigureAwait(false)) > 0)
        {
            hasher.AppendData(buffer, 0, read);
        }

        var final = hasher.GetHashAndReset();
        return Convert.ToHexString(final).ToLowerInvariant();
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
