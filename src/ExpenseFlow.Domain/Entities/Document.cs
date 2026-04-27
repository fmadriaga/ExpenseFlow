namespace ExpenseFlow.Domain.Entities;

public class Document
{
    public int Id { get; set; }

    public string FilePath { get; set; } = string.Empty;

    public string FileHash { get; set; } = string.Empty;

    public string? MerchantName { get; set; }

    public DateOnly? TransactionDate { get; set; }

    public string? Currency { get; set; }

    public decimal? TotalAmount { get; set; }

    public decimal? TaxAmount { get; set; }

    public decimal Confidence { get; set; }

    public string? RawJson { get; set; }

    public string? OcrStatus { get; set; }

    public string? ErrorMessage { get; set; }

    public string Category { get; set; } = "otros";

    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<DocumentLine> DocumentLines { get; set; } = new List<DocumentLine>();

    public ICollection<ProcessingJob> ProcessingJobs { get; set; } = new List<ProcessingJob>();
}
