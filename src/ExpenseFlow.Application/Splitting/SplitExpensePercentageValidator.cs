namespace ExpenseFlow.Application.Splitting;

/// <summary>
/// Valida que los porcentajes de un reparto sumen 100% (redondeo a 2 decimales).
/// </summary>
public static class SplitExpensePercentageValidator
{
    public const decimal Hundred = 100m;

    public static void EnsureSumIsHundred(IReadOnlyList<decimal> percentages)
    {
        if (percentages is null)
        {
            throw new ArgumentNullException(nameof(percentages));
        }

        if (percentages.Count == 0)
        {
            throw new ArgumentException("Debe existir al menos un porcentaje en el reparto.");
        }

        var sum = Math.Round(percentages.Sum(p => p), 2, MidpointRounding.AwayFromZero);
        if (sum != Hundred)
        {
            throw new ArgumentException(
                $"Los porcentajes deben sumar exactamente 100% (suma actual: {sum}).");
        }
    }
}
