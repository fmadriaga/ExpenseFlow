namespace ExpenseFlow.Application.FileScanning;

/// <param name="FullPath">Ruta completa al archivo en disco.</param>
/// <param name="FileHash">Hash SHA-256 (hex) del contenido.</param>
/// <param name="IsAlreadyInDatabase">Indica si ya existía en base un <c>Document</c> con el mismo <c>FileHash</c>.</param>
public record ScanResult(
    string FullPath,
    string FileHash,
    bool IsAlreadyInDatabase);
