namespace ExpenseFlow.Domain.Entities;

public class Family
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Ruta de inbox (relativa al ContentRoot del Worker o absoluta), misma semántica que <c>Storage:Inbox</c>.
    /// </summary>
    public string InboxPath { get; set; } = string.Empty;

    public string ProcessedPath { get; set; } = string.Empty;

    public string ErrorPath { get; set; } = string.Empty;

    public ICollection<Document> Documents { get; set; } = new List<Document>();

    public ICollection<FamilyMember> Members { get; set; } = new List<FamilyMember>();
}
