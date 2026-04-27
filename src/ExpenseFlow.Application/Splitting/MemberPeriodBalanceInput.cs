namespace ExpenseFlow.Application.Splitting;

/// <summary>
/// Datos de un documento con reparto, para cálculo de balance en memoria.
/// </summary>
public sealed class MemberPeriodBalanceInput
{
    public int DocumentId { get; init; }

    public decimal? TotalAmount { get; init; }

    public int? PaidByFamilyMemberId { get; init; }

    public IReadOnlyList<MemberPeriodSplitLine> Splits { get; init; } = Array.Empty<MemberPeriodSplitLine>();
}

public sealed class MemberPeriodSplitLine
{
    public int FamilyMemberId { get; init; }

    public decimal Percentage { get; init; }
}
