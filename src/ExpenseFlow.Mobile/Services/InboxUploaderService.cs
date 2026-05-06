using Microsoft.Extensions.Configuration;

namespace ExpenseFlow.Mobile.Services;

public sealed class InboxUploaderService
{
    private readonly IConfiguration _configuration;

    public InboxUploaderService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<string> UploadAsync(Stream photoStream, CancellationToken ct = default)
    {
        var inboxLocalPath = _configuration["ExpenseFlow:InboxLocalPath"];
        if (string.IsNullOrWhiteSpace(inboxLocalPath))
        {
            throw new InvalidOperationException(
                "Configuration key 'ExpenseFlow:InboxLocalPath' is required to upload photos.");
        }

        Directory.CreateDirectory(inboxLocalPath);

        var fileName = $"ticket_{DateTime.UtcNow:yyyyMMdd_HHmmss_fff}.jpg";
        var fullPath = Path.Combine(inboxLocalPath, fileName);

        await using var fileStream = new FileStream(
            fullPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None);

        await photoStream.CopyToAsync(fileStream, ct);
        await fileStream.FlushAsync(ct);

        return fullPath;
    }
}
