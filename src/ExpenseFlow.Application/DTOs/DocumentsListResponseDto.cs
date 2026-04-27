namespace ExpenseFlow.Application.DTOs;

public sealed class DocumentsListResponseDto
{
    public IReadOnlyList<DocumentSummaryDto> Items { get; set; } = Array.Empty<DocumentSummaryDto>();

    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalCount { get; set; }
}
