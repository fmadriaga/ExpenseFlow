namespace ExpenseFlow.Application.FileScanning;

/// <param name="FullPath">Ruta completa al archivo en disco.</param>
/// <param name="FileHash">Hash SHA-256 (hex) del contenido.</param>
/// <param name="IsAlreadyInDatabase">Indica si ya existía en base un <c>Document</c> con el mismo <c>FileHash</c>.</param>
/// <param name="FamilyId">Perfil cuyo inbox se escaneó.</param>
/// <param name="ProcessedStorageRoot">Raíz absoluta de procesados para esta familia.</param>
/// <param name="ErrorStorageRoot">Raíz absoluta de errores para esta familia.</param>
public record ScanResult(
    string FullPath,
    string FileHash,
    bool IsAlreadyInDatabase,
    int FamilyId,
    string ProcessedStorageRoot,
    string ErrorStorageRoot);
