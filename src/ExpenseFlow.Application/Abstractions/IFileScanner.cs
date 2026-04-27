using ExpenseFlow.Application.FileScanning;

namespace ExpenseFlow.Application.Abstractions;

public interface IFileScanner
{
    /// <summary>
    /// Recorre el inbox indicado, ignora no válidos y devuelve candidatos cuyo hash aún no está
    /// en éxito para la misma <paramref name="familyId"/>.
    /// </summary>
    Task<IReadOnlyList<ScanResult>> GetPendingFilesToProcessAsync(
        int familyId,
        string inboxAbsolutePath,
        string processedStorageRoot,
        string errorStorageRoot,
        CancellationToken cancellationToken = default);
}
