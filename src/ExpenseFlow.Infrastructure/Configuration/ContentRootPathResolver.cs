namespace ExpenseFlow.Infrastructure.Configuration;

/// <summary>
/// Resuelve rutas de almacenamiento relativas al <c>ContentRoot</c> del host (misma semántica que <see cref="Options.PostConfigureStoragePaths"/>).
/// </summary>
public static class ContentRootPathResolver
{
    public static string Resolve(string contentRoot, string configured)
    {
        if (string.IsNullOrWhiteSpace(configured))
        {
            throw new ArgumentException("Configured path is required.", nameof(configured));
        }

        return Path.IsPathRooted(configured)
            ? Path.GetFullPath(configured)
            : Path.GetFullPath(Path.Combine(contentRoot, configured));
    }
}
