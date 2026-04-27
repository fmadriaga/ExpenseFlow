namespace ExpenseFlow.Application.DTOs;

/// <summary>
/// Actualización parcial manual de campos extraídos por OCR (TASK-019).
/// Solo los miembros no null sobrescriben el documento.
/// </summary>
public sealed class PatchDocumentRequestDto
{
    public string? MerchantName { get; set; }

    public DateOnly? TransactionDate { get; set; }

    public decimal? TotalAmount { get; set; }

    public string? Category { get; set; }
}
