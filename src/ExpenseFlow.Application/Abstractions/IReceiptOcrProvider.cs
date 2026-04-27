using ExpenseFlow.Application.Ocr;

namespace ExpenseFlow.Application.Abstractions;

public interface IReceiptOcrProvider
{
    Task<OcrResult> AnalyzeReceiptAsync(
        string filePath,
        CancellationToken cancellationToken = default);
}
