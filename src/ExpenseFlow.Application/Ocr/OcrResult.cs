namespace ExpenseFlow.Application.Ocr;

public sealed record OcrResult(
    string? MerchantName,
    DateOnly? TransactionDate,
    decimal? TotalAmount,
    decimal? TaxAmount,
    string RawJson,
    IReadOnlyList<OcrLineItem> Lines,
    string? Currency = null);

public sealed record OcrLineItem(
    string? Description,
    decimal? Quantity,
    decimal? UnitPrice,
    decimal? TotalPrice);
