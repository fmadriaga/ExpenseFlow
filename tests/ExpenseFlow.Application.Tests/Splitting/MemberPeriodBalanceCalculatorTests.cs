using ExpenseFlow.Application.Splitting;
using Xunit;

namespace ExpenseFlow.Application.Tests.Splitting;

public sealed class MemberPeriodBalanceCalculatorTests
{
    private static readonly DateOnly From = new(2026, 1, 1);
    private static readonly DateOnly To = new(2026, 12, 31);

    [Fact]
    public void Member_who_paid_gets_positive_net_when_share_is_lower()
    {
        var docs = new List<MemberPeriodBalanceInput>
        {
            new()
            {
                DocumentId = 1,
                TotalAmount = 100m,
                PaidByFamilyMemberId = 1,
                Splits = new List<MemberPeriodSplitLine>
                {
                    new() { FamilyMemberId = 1, Percentage = 40m },
                    new() { FamilyMemberId = 2, Percentage = 60m },
                },
            },
        };
        var r1 = MemberPeriodBalanceCalculator.Compute(1, "M1", From, To, docs);
        Assert.Equal(100m, r1.TotalPaid);
        Assert.Equal(40m, r1.TotalShare);
        Assert.Equal(60m, r1.Net);
    }

    [Fact]
    public void Other_member_gets_negative_net_when_they_did_not_pay()
    {
        var docs = new List<MemberPeriodBalanceInput>
        {
            new()
            {
                DocumentId = 1,
                TotalAmount = 100m,
                PaidByFamilyMemberId = 1,
                Splits = new List<MemberPeriodSplitLine>
                {
                    new() { FamilyMemberId = 1, Percentage = 40m },
                    new() { FamilyMemberId = 2, Percentage = 60m },
                },
            },
        };
        var r2 = MemberPeriodBalanceCalculator.Compute(2, "M2", From, To, docs);
        Assert.Equal(0m, r2.TotalPaid);
        Assert.Equal(60m, r2.TotalShare);
        Assert.Equal(-60m, r2.Net);
    }

    [Fact]
    public void Skips_documents_without_total_amount()
    {
        var docs = new List<MemberPeriodBalanceInput>
        {
            new()
            {
                DocumentId = 1,
                TotalAmount = null,
                PaidByFamilyMemberId = 1,
                Splits = new List<MemberPeriodSplitLine> { new() { FamilyMemberId = 1, Percentage = 100m } },
            },
        };
        var r = MemberPeriodBalanceCalculator.Compute(1, "M1", From, To, docs);
        Assert.Equal(0m, r.TotalPaid);
        Assert.Equal(0m, r.TotalShare);
        Assert.Equal(0m, r.Net);
    }

    [Fact]
    public void Empty_documents_yields_zero_balances()
    {
        var r = MemberPeriodBalanceCalculator.Compute(1, "M1", From, To, Array.Empty<MemberPeriodBalanceInput>());
        Assert.Equal(0m, r.TotalPaid);
        Assert.Equal(0m, r.TotalShare);
        Assert.Equal(0m, r.Net);
    }
}
