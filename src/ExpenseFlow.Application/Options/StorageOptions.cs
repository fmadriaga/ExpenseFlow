using System.ComponentModel.DataAnnotations;

namespace ExpenseFlow.Application.Options;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    [Required(AllowEmptyStrings = false)]
    public string Inbox { get; set; } = "../../storage/familia/inbox";

    [Required(AllowEmptyStrings = false)]
    public string Processed { get; set; } = "../../storage/familia/processed";

    [Required(AllowEmptyStrings = false)]
    public string Error { get; set; } = "../../storage/familia/error";
}
