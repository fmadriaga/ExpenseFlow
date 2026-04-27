namespace ExpenseFlow.Application.DTOs;

public sealed class DocumentDetailDto
{
    public int Id { get; set; }

    public string FilePath { get; set; } = string.Empty;

    public string? MerchantName { get; set; }

    public DateOnly? TransactionDate { get; set; }

    public decimal? TotalAmount { get; set; }

    public string? Currency { get; set; }

    public string? Category { get; set; }

    public string? OcrStatus { get; set; }

    public decimal Confidence { get; set; }

    public string? RawJson { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public IReadOnlyList<DocumentLineDto> Lines { get; set; } = Array.Empty<DocumentLineDto>();
}
