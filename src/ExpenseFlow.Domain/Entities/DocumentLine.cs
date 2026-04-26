namespace ExpenseFlow.Domain.Entities;

public class DocumentLine
{
    public int Id { get; set; }

    public int DocumentId { get; set; }

    public string? Description { get; set; }

    public decimal Amount { get; set; }

    public string? Currency { get; set; }

    public Document? Document { get; set; }
}
