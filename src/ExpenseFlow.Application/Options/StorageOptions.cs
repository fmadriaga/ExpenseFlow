namespace ExpenseFlow.Application.Options;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    public string Inbox { get; set; } = "../../storage/familia/inbox";

    public string Processed { get; set; } = "../../storage/familia/processed";

    public string Error { get; set; } = "../../storage/familia/error";
}
