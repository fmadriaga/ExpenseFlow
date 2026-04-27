namespace ExpenseFlow.Domain.Entities;

public class Document
{
    public int Id { get; set; }

    public int FamilyId { get; set; }

    public Family Family { get; set; } = null!;

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

    /// <summary>
    /// Quién pagó el ticket (miembro de la misma familia). Obligatorio cuando hay <see cref="ExpenseSplits"/>.
    /// </summary>
    public int? PaidByFamilyMemberId { get; set; }

    public FamilyMember? PaidByMember { get; set; }

    public ICollection<DocumentLine> DocumentLines { get; set; } = new List<DocumentLine>();

    public ICollection<ExpenseSplit> ExpenseSplits { get; set; } = new List<ExpenseSplit>();

    public ICollection<ProcessingJob> ProcessingJobs { get; set; } = new List<ProcessingJob>();
}
