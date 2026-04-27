namespace ExpenseFlow.Application.DTOs;

public sealed class SetDocumentSplitRequestDto
{
    public int PaidByFamilyMemberId { get; set; }

    public IReadOnlyList<SplitLineRequestDto> Splits { get; set; } = Array.Empty<SplitLineRequestDto>();
}

public sealed class SplitLineRequestDto
{
    public int FamilyMemberId { get; set; }

    public decimal Percentage { get; set; }
}
