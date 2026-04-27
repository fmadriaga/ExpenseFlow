namespace ExpenseFlow.Application.Abstractions;

/// <summary>
/// Mueve archivos del inbox a las carpetas configuradas bajo <c>processed</c> o <c>error</c>,
/// organizados por <c>yyyy/MM</c> (UTC). No consulta base de datos; la deduplicación por hash
/// corresponde a <see cref="IFileScanner"/>.
/// </summary>
public interface IFileMover
{
    /// <summary>
    /// Mueve usando las rutas raíz de la sección <c>Storage</c> del host (Api/reproceso compat).
    /// </summary>
    Task<string> MoveToProcessedAsync(string sourcePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Mueve el archivo a <c>{processedStorageRoot}/yyyy/MM/</c>.
    /// </summary>
    Task<string> MoveToProcessedAsync(
        string sourcePath,
        string processedStorageRoot,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Mueve usando la raíz <c>Error</c> de <c>Storage</c>.
    /// </summary>
    Task<string> MoveToErrorAsync(string sourcePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Mueve el archivo a <c>{errorStorageRoot}/yyyy/MM/</c>.
    /// </summary>
    Task<string> MoveToErrorAsync(
        string sourcePath,
        string errorStorageRoot,
        CancellationToken cancellationToken = default);
}
