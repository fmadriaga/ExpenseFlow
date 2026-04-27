namespace ExpenseFlow.Application.Splitting;

/// <summary>
/// Calcula balance de un miembro a partir de documentos con reparto (sin acceso a EF).
/// </summary>
public static class MemberPeriodBalanceCalculator
{
    public static MemberPeriodBalanceResult Compute(
        int familyMemberId,
        string familyMemberName,
        DateOnly from,
        DateOnly to,
        IReadOnlyList<MemberPeriodBalanceInput> documents)
    {
        if (documents is null)
        {
            throw new ArgumentNullException(nameof(documents));
        }

        var totalPaid = 0m;
        var totalShare = 0m;

        foreach (var doc in documents)
        {
            if (doc.Splits.Count == 0)
            {
                continue;
            }

            if (doc.TotalAmount is not { } amount)
            {
                continue;
            }

            if (doc.PaidByFamilyMemberId == familyMemberId)
            {
                totalPaid = decimal.Round(totalPaid + amount, 2, MidpointRounding.AwayFromZero);
            }

            var line = doc.Splits.FirstOrDefault(s => s.FamilyMemberId == familyMemberId);
            if (line is not null)
            {
                var part = amount * (line.Percentage / 100m);
                totalShare = decimal.Round(totalShare + part, 2, MidpointRounding.AwayFromZero);
            }
        }

        var net = decimal.Round(totalPaid - totalShare, 2, MidpointRounding.AwayFromZero);
        return new MemberPeriodBalanceResult
        {
            FamilyMemberId = familyMemberId,
            FamilyMemberName = familyMemberName,
            From = from,
            To = to,
            TotalPaid = totalPaid,
            TotalShare = totalShare,
            Net = net,
        };
    }
}
