namespace ExpenseFlow.Application.Options;

public sealed class WorkerOptions
{
    public const string SectionName = "Worker";

    /// <summary>
    /// Segundos de espera entre el fin de un ciclo y el inicio del siguiente.
    /// </summary>
    public int IntervalSeconds { get; set; } = 60;
}
