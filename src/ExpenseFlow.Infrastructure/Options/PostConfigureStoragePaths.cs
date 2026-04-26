using ExpenseFlow.Application.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace ExpenseFlow.Infrastructure.Options;

public sealed class PostConfigureStoragePaths : IPostConfigureOptions<StorageOptions>
{
    private readonly IHostEnvironment _hostEnvironment;

    public PostConfigureStoragePaths(IHostEnvironment hostEnvironment) =>
        _hostEnvironment = hostEnvironment;

    public void PostConfigure(string? name, StorageOptions options)
    {
        options.Inbox = Resolve(
            string.IsNullOrWhiteSpace(options.Inbox) ? null : options.Inbox,
            "../../storage/familia/inbox");
        options.Processed = Resolve(
            string.IsNullOrWhiteSpace(options.Processed) ? null : options.Processed,
            "../../storage/familia/processed");
        options.Error = Resolve(
            string.IsNullOrWhiteSpace(options.Error) ? null : options.Error,
            "../../storage/familia/error");
    }

    private string Resolve(string? configured, string defaultRelativeToContentRoot) =>
        string.IsNullOrWhiteSpace(configured)
            ? Path.GetFullPath(
                Path.Combine(
                    _hostEnvironment.ContentRootPath,
                    defaultRelativeToContentRoot))
            : Path.IsPathRooted(configured)
                ? Path.GetFullPath(configured)
                : Path.GetFullPath(Path.Combine(_hostEnvironment.ContentRootPath, configured));
}
