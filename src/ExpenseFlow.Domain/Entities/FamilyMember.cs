namespace ExpenseFlow.Domain.Entities;

public class FamilyMember
{
    public int Id { get; set; }

    public int FamilyId { get; set; }

    public Family Family { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public ICollection<ExpenseSplit> ExpenseSplits { get; set; } = new List<ExpenseSplit>();
}
