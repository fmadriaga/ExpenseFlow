namespace ExpenseFlow.Application.DTOs;

public sealed class DocumentSummaryDto
{
    public int Id { get; set; }

    public string? MerchantName { get; set; }

    public DateOnly? TransactionDate { get; set; }

    public decimal? TotalAmount { get; set; }

    public string? Currency { get; set; }

    public string? Category { get; set; }

    public string? OcrStatus { get; set; }

    public decimal Confidence { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
