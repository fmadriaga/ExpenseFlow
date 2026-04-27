namespace ExpenseFlow.Application.Abstractions;

/// <summary>
/// Busca ficheros bajo la carpeta de error y los restaura al inbox sin sobrescribir destinos existentes.
/// </summary>
public interface IFileRestorer
{
    /// <summary>
    /// Busca recursivamente bajo la raíz de error un archivo cuyo SHA-256 (hex) coincide con <paramref name="fileHashHex"/>.
    /// </summary>
    Task<string?> FindSourcePathInErrorTreeAsync(string fileHashHex, CancellationToken cancellationToken = default);

    /// <summary>
    /// Mueve un archivo desde una ruta existente bajo error hacia el inbox (primer nivel).
    /// Devuelve la ruta destino final o null si el origen no existe.
    /// </summary>
    Task<string?> RestoreToInboxAsync(string sourcePath, CancellationToken cancellationToken = default);
}
