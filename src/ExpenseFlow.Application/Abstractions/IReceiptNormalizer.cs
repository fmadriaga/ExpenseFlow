using ExpenseFlow.Application.Ocr;
using ExpenseFlow.Domain.Entities;

namespace ExpenseFlow.Application.Abstractions;

public interface IReceiptNormalizer
{
    Document Normalize(OcrResult ocrResult, string filePath, string fileHash);
}
