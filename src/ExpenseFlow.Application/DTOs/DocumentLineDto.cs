namespace ExpenseFlow.Application.DTOs;

public sealed class DocumentLineDto
{
    public string? Description { get; set; }

    public decimal? Quantity { get; set; }

    public decimal? UnitPrice { get; set; }

    public decimal Amount { get; set; }

    public string? Currency { get; set; }
}
