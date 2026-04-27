namespace ExpenseFlow.Application.Export;

/// <summary>
/// Escribe filas de documentos en CSV (RFC 4180: comillas y delimitador).
/// </summary>
public interface ICsvExporter
{
    /// <summary>
    /// Escribe encabezado y filas; <paramref name="writer"/> usa fin de línea CRLF.
    /// </summary>
    Task WriteDocumentExportAsync(
        IAsyncEnumerable<DocumentExportRow> rows,
        TextWriter writer,
        char delimiter,
        CancellationToken cancellationToken = default);
}
