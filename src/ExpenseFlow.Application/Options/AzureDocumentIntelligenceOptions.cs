using System.ComponentModel.DataAnnotations;

namespace ExpenseFlow.Application.Options;

public sealed class AzureDocumentIntelligenceOptions
{
    public const string SectionName = "AzureDocumentIntelligence";

    [Required(AllowEmptyStrings = false)]
    public string Endpoint { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string ApiKey { get; set; } = string.Empty;
}
