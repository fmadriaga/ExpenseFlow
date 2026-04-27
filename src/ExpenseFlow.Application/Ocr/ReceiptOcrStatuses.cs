namespace ExpenseFlow.Application.Ocr;

public static class ReceiptOcrStatuses
{
    public const string Success = "Success";

    public const string Partial = "Partial";

    public const string Failed = "Failed";

    /// <summary>
    /// Marcado manual para reproceso; el Worker debe volver a procesar el mismo fichero por hash.
    /// </summary>
    public const string Pending = "Pending";
}
