namespace ExpenseFlow.Application.Abstractions;

/// <summary>
/// Mueve archivos del inbox a las carpetas configuradas bajo <c>processed</c> o <c>error</c>,
/// organizados por <c>yyyy/MM</c> (UTC). No consulta base de datos; la deduplicación por hash
/// corresponde a <see cref="IFileScanner"/>.
/// </summary>
public interface IFileMover
{
    /// <summary>
    /// Mueve el archivo a <c>{Processed}/yyyy/MM/</c> y devuelve la ruta final (puede diferir
    /// del nombre preferido si hubo colisión).
    /// </summary>
    Task<string> MoveToProcessedAsync(string sourcePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Mueve el archivo a <c>{Error}/yyyy/MM/</c> y devuelve la ruta final.
    /// </summary>
    Task<string> MoveToErrorAsync(string sourcePath, CancellationToken cancellationToken = default);
}
