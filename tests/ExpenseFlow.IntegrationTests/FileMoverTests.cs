using ExpenseFlow.Application.Options;
using ExpenseFlow.Infrastructure.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace ExpenseFlow.IntegrationTests;

public class FileMoverTests
{
    [Fact]
    public async Task MoveToProcessedAsync_moves_file_under_yyyy_MM_and_removes_source()
    {
        var root = CreateTempRoot();
        var processed = Path.Combine(root, "processed");
        var error = Path.Combine(root, "error");
        var inbox = Path.Combine(root, "inbox");
        Directory.CreateDirectory(inbox);
        var source = Path.Combine(inbox, "ticket.txt");
        await File.WriteAllTextAsync(source, "ok");

        try
        {
            var mover = CreateMover(processed, error);
            var dest = await mover.MoveToProcessedAsync(source);

            Assert.False(File.Exists(source));
            Assert.True(File.Exists(dest));
            Assert.Equal("ok", await File.ReadAllTextAsync(dest));

            var utc = DateTime.UtcNow;
            Assert.StartsWith(Path.GetFullPath(processed), Path.GetFullPath(dest), StringComparison.Ordinal);
            Assert.Contains(Path.Combine(utc.ToString("yyyy"), utc.ToString("MM")), dest);
        }
        finally
        {
            TryDeleteDirectory(root);
        }
    }

    [Fact]
    public async Task MoveToErrorAsync_moves_file_under_yyyy_MM()
    {
        var root = CreateTempRoot();
        var processed = Path.Combine(root, "processed");
        var error = Path.Combine(root, "error");
        var inbox = Path.Combine(root, "inbox");
        Directory.CreateDirectory(inbox);
        var source = Path.Combine(inbox, "bad.pdf");
        await File.WriteAllBytesAsync(source, [1, 2, 3]);

        try
        {
            var mover = CreateMover(processed, error);
            var dest = await mover.MoveToErrorAsync(source);

            Assert.False(File.Exists(source));
            Assert.True(File.Exists(dest));
            Assert.Equal(new byte[] { 1, 2, 3 }, await File.ReadAllBytesAsync(dest));
            Assert.StartsWith(Path.GetFullPath(error), Path.GetFullPath(dest), StringComparison.Ordinal);
        }
        finally
        {
            TryDeleteDirectory(root);
        }
    }

    [Fact]
    public async Task MoveToProcessedAsync_uses_unique_name_when_destination_exists()
    {
        var root = CreateTempRoot();
        var processed = Path.Combine(root, "processed");
        var error = Path.Combine(root, "error");
        var inbox = Path.Combine(root, "inbox");
        Directory.CreateDirectory(inbox);

        var utc = DateTime.UtcNow;
        var monthDir = Path.Combine(processed, utc.ToString("yyyy"), utc.ToString("MM"));
        Directory.CreateDirectory(monthDir);
        var colliding = Path.Combine(monthDir, "dup.txt");
        await File.WriteAllTextAsync(colliding, "existing");

        var source = Path.Combine(inbox, "dup.txt");
        await File.WriteAllTextAsync(source, "incoming");

        try
        {
            var mover = CreateMover(processed, error);
            var dest = await mover.MoveToProcessedAsync(source);

            Assert.False(File.Exists(source));
            Assert.True(File.Exists(colliding));
            Assert.True(File.Exists(dest));
            Assert.NotEqual(
                Path.GetFullPath(colliding),
                Path.GetFullPath(dest),
                StringComparer.OrdinalIgnoreCase);
            Assert.Equal("existing", await File.ReadAllTextAsync(colliding));
            Assert.Equal("incoming", await File.ReadAllTextAsync(dest));
        }
        finally
        {
            TryDeleteDirectory(root);
        }
    }

    [Fact]
    public async Task MoveToProcessedAsync_creates_destination_directory_tree()
    {
        var root = CreateTempRoot();
        var processed = Path.Combine(root, "processed");
        var error = Path.Combine(root, "error");
        var inbox = Path.Combine(root, "inbox");
        Directory.CreateDirectory(inbox);
        var source = Path.Combine(inbox, "a.png");
        await File.WriteAllTextAsync(source, "x");

        Assert.False(Directory.Exists(Path.Combine(processed, DateTime.UtcNow.ToString("yyyy"))));

        try
        {
            var mover = CreateMover(processed, error);
            var dest = await mover.MoveToProcessedAsync(source);

            Assert.True(File.Exists(dest));
            Assert.True(Directory.Exists(Path.GetDirectoryName(dest)!));
        }
        finally
        {
            TryDeleteDirectory(root);
        }
    }

    private static string CreateTempRoot() =>
        Path.Combine(Path.GetTempPath(), "expenseflow_filemover_" + Guid.NewGuid().ToString("N"));

    private static FileMover CreateMover(string processedRoot, string errorRoot) =>
        new(
            Options.Create(
                new StorageOptions
                {
                    Inbox = string.Empty,
                    Processed = processedRoot,
                    Error = errorRoot,
                }),
            NullLogger<FileMover>.Instance);

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch
        {
            // best effort cleanup for temp tests
        }
    }
}
