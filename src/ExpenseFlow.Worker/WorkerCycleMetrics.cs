using System.Diagnostics.Metrics;

namespace ExpenseFlow.Worker;

/// <summary>
/// Contadores por ciclo de job; visibles con <c>dotnet-counters</c> (meter <c>ExpenseFlow.Worker</c>).
/// </summary>
internal static class WorkerCycleMetrics
{
    private static readonly Meter Meter = new("ExpenseFlow.Worker", "1.0.0");

    internal static readonly Counter<long> FilesFound = Meter.CreateCounter<long>(
        "files.found",
        unit: "{file}",
        description: "Candidatos devueltos por el escáner en un ciclo");

    internal static readonly Counter<long> FilesProcessedOk = Meter.CreateCounter<long>(
        "files.processed_ok",
        unit: "{file}",
        description: "Archivos persistidos y movidos a processed con éxito");

    internal static readonly Counter<long> FilesProcessedFailed = Meter.CreateCounter<long>(
        "files.processed_failed",
        unit: "{file}",
        description: "Archivos que terminaron en error (OCR, persistencia, etc.)");
}
