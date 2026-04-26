namespace ExpenseFlow.Domain.Entities;

public class ProcessingJob
{
    public int Id { get; set; }

    public int DocumentId { get; set; }

    public DateTimeOffset? StartedAt { get; set; }

    public DateTimeOffset? FinishedAt { get; set; }

    public string? Status { get; set; }

    public string? ErrorMessage { get; set; }

    public Document? Document { get; set; }
}
