namespace ExpenseFlow.Application.Export;

/// <summary>
/// Fila plana para exportación CSV (histórico de documentos).
/// </summary>
public sealed record DocumentExportRow(
    int Id,
    string FilePath,
    string? MerchantName,
    DateOnly? TransactionDate,
    decimal? TotalAmount,
    decimal? TaxAmount,
    string? Currency,
    string Category,
    string? OcrStatus,
    DateTimeOffset CreatedAt);
