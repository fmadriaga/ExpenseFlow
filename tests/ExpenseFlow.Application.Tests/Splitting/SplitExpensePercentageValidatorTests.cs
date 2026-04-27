using ExpenseFlow.Application.Splitting;
using Xunit;

namespace ExpenseFlow.Application.Tests.Splitting;

public sealed class SplitExpensePercentageValidatorTests
{
    [Fact]
    public void Accepts_fifty_fifty() =>
        SplitExpensePercentageValidator.EnsureSumIsHundred(new[] { 50m, 50m });

    [Fact]
    public void Accepts_three_lines_rounding_to_100() =>
        SplitExpensePercentageValidator.EnsureSumIsHundred(new[] { 33.33m, 33.33m, 33.34m });

    [Fact]
    public void Rejects_when_sum_not_100()
    {
        var ex = Assert.Throws<ArgumentException>(
            () => SplitExpensePercentageValidator.EnsureSumIsHundred(new[] { 50m, 40m }));
        Assert.Contains("100", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Rejects_empty_list() =>
        Assert.Throws<ArgumentException>(
            () => SplitExpensePercentageValidator.EnsureSumIsHundred(Array.Empty<decimal>()));
}
