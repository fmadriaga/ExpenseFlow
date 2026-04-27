using System.ComponentModel.DataAnnotations;

namespace ExpenseFlow.Application.Options;

public sealed class AzureDocumentIntelligenceOptions
{
    public const string SectionName = "AzureDocumentIntelligence";

    [Required(AllowEmptyStrings = false)]
    public string Endpoint { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Reintentos <em>adicionales</em> tras el primer fallo transitorio (0 = solo un intento).
    /// </summary>
    [Range(0, 20)]
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Retraso base (segundos) antes del primer reintento; luego backoff exponencial (2^intento).
    /// </summary>
    [Range(0.1, 300)]
    public double BaseDelaySeconds { get; set; } = 1.0;
}
