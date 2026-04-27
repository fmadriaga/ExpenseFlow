namespace ExpenseFlow.Application.Splitting;

public sealed class MemberPeriodBalanceResult
{
    public int FamilyMemberId { get; init; }

    public string FamilyMemberName { get; init; } = string.Empty;

    public DateOnly From { get; init; }

    public DateOnly To { get; init; }

    /// <summary>Total facturado en documentos que este miembro pagó (con reparto registrado).</summary>
    public decimal TotalPaid { get; init; }

    /// <summary>Total que corresponde a este miembro según porcentajes (y TotalAmount de cada documento).</summary>
    public decimal TotalShare { get; init; }

    /// <summary>TotalPaid − TotalShare. Positivo: pagó más de su parte imputable.</summary>
    public decimal Net { get; init; }
}
