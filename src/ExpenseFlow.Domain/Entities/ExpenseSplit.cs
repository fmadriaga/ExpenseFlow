namespace ExpenseFlow.Domain.Entities;

public class ExpenseSplit
{
    public int Id { get; set; }

    public int DocumentId { get; set; }

    public Document Document { get; set; } = null!;

    public int FamilyMemberId { get; set; }

    public FamilyMember FamilyMember { get; set; } = null!;

    /// <summary>Parte atribuible a este miembro (0–100, dos decimales en BD).</summary>
    public decimal Percentage { get; set; }
}
