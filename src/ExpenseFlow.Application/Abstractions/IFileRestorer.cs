namespace ExpenseFlow.Application.Abstractions;

/// <summary>
/// Busca ficheros bajo la carpeta de error y los restaura al inbox sin sobrescribir destinos existentes.
/// </summary>
public interface IFileRestorer
{
    /// <summary>
    /// Busca bajo <c>Storage:Error</c> del host.
    /// </summary>
    Task<string?> FindSourcePathInErrorTreeAsync(string fileHashHex, CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca recursivamente bajo <paramref name="errorStorageRoot"/> un archivo cuyo SHA-256 coincide con el hash.
    /// </summary>
    Task<string?> FindSourcePathInErrorTreeAsync(
        string fileHashHex,
        string errorStorageRoot,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Restaura al inbox configurado en <c>Storage:Inbox</c>.
    /// </summary>
    Task<string?> RestoreToInboxAsync(string sourcePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Mueve el archivo al inbox bajo <paramref name="inboxStorageRoot"/>.
    /// </summary>
    Task<string?> RestoreToInboxAsync(
        string sourcePath,
        string inboxStorageRoot,
        CancellationToken cancellationToken = default);
}
