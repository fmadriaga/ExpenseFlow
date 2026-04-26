using ExpenseFlow.Application.FileScanning;

namespace ExpenseFlow.Application.Abstractions;

public interface IFileScanner
{
    /// <summary>
    /// Recorre el inbox, ignora no válidos y devuelve solo archivos aún no registrados
    /// en base (candidatos a procesar con <see cref="ScanResult.IsAlreadyInDatabase"/> falso).
    /// </summary>
    Task<IReadOnlyList<ScanResult>> GetPendingFilesToProcessAsync(
        CancellationToken cancellationToken = default);
}
