using ExpenseFlow.Application.Abstractions;
using ExpenseFlow.Application.Ocr;
using ExpenseFlow.Domain.Entities;

namespace ExpenseFlow.Application.Services;

public sealed class ReceiptNormalizer : IReceiptNormalizer
{
    private const decimal KeyFieldSlots = 6m;

    public Document Normalize(OcrResult ocrResult, string filePath, string fileHash)
    {
        ArgumentNullException.ThrowIfNull(ocrResult);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileHash);

        var document = new Document
        {
            FilePath = filePath,
            FileHash = fileHash,
            RawJson = ocrResult.RawJson,
            MerchantName = string.IsNullOrWhiteSpace(ocrResult.MerchantName)
                ? null
                : NormalizeMerchantName(ocrResult.MerchantName),
            TransactionDate = ocrResult.TransactionDate,
            Currency = string.IsNullOrWhiteSpace(ocrResult.Currency)
                ? null
                : ocrResult.Currency.Trim(),
            TotalAmount = ocrResult.TotalAmount,
            TaxAmount = ocrResult.TaxAmount,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var presentSlots = CountPresentKeyFields(ocrResult);
        document.Confidence = Math.Round(
            100m * presentSlots / KeyFieldSlots,
            2,
            MidpointRounding.AwayFromZero);
        document.OcrStatus = ResolveStatus(ocrResult, document);

        foreach (var line in ocrResult.Lines)
        {
            document.DocumentLines.Add(
                new DocumentLine
                {
                    Description = string.IsNullOrWhiteSpace(line.Description)
                        ? null
                        : line.Description.Trim(),
                    Quantity = line.Quantity,
                    UnitPrice = line.UnitPrice,
                    Amount = ComputeLineAmount(line),
                    Currency = document.Currency,
                });
        }

        return document;
    }

    private static int CountPresentKeyFields(OcrResult ocrResult)
    {
        var n = 0;
        if (!string.IsNullOrWhiteSpace(ocrResult.MerchantName))
        {
            n++;
        }

        if (ocrResult.TransactionDate.HasValue)
        {
            n++;
        }

        if (ocrResult.TotalAmount.HasValue)
        {
            n++;
        }

        if (ocrResult.TaxAmount.HasValue)
        {
            n++;
        }

        if (!string.IsNullOrWhiteSpace(ocrResult.Currency))
        {
            n++;
        }

        if (ocrResult.Lines.Count > 0)
        {
            n++;
        }

        return n;
    }

    private static string ResolveStatus(OcrResult ocrResult, Document document)
    {
        var hasMinimum =
            !string.IsNullOrWhiteSpace(document.MerchantName) ||
            document.TotalAmount.HasValue;

        if (hasMinimum)
        {
            return ReceiptOcrStatuses.Success;
        }

        var hasSecondary =
            document.TransactionDate.HasValue ||
            document.TaxAmount.HasValue ||
            !string.IsNullOrWhiteSpace(document.Currency) ||
            ocrResult.Lines.Count > 0;

        return hasSecondary ? ReceiptOcrStatuses.Partial : ReceiptOcrStatuses.Failed;
    }

    /// <summary>
    /// Collapses embedded line-breaks and redundant whitespace in OCR merchant names.
    /// Azure Document Intelligence returns multi-line receipt headers as a single string
    /// with '\n' separators (e.g. "farmacias\nPIGALLE"). This normalizes them to a single
    /// space-separated value so the name displays cleanly and keyword matching works correctly.
    /// </summary>
    private static string NormalizeMerchantName(string raw) =>
        string.Join(
            ' ',
            raw.Split(['\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
           .Trim();

    private static decimal ComputeLineAmount(OcrLineItem line)
    {
        if (line.TotalPrice.HasValue)
        {
            return line.TotalPrice.Value;
        }

        if (line.Quantity.HasValue && line.UnitPrice.HasValue)
        {
            return line.Quantity.Value * line.UnitPrice.Value;
        }

        return 0m;
    }
}
