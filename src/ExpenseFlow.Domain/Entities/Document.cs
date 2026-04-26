namespace ExpenseFlow.Domain.Entities;

public class Document
{
    public int Id { get; set; }

    public string FilePath { get; set; } = string.Empty;

    public string FileHash { get; set; } = string.Empty;

    public string? RawJson { get; set; }

    public string? OcrStatus { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<DocumentLine> DocumentLines { get; set; } = new List<DocumentLine>();

    public ICollection<ProcessingJob> ProcessingJobs { get; set; } = new List<ProcessingJob>();
}
