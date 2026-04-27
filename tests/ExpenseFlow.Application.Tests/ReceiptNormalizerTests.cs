using ExpenseFlow.Application.Ocr;
using ExpenseFlow.Application.Services;
using Xunit;
using System.Linq;

namespace ExpenseFlow.Application.Tests;

public class ReceiptNormalizerTests
{
    private readonly ReceiptNormalizer _normalizer = new();

    [Fact]
    public void Normalize_maps_all_fields_success_status_lines_and_confidence()
    {
        const string raw = """{"ok":true}""";
        var lines = new[]
        {
            new OcrLineItem("Item A", 2m, 3m, 6m),
        };
        var ocr = new OcrResult(
            MerchantName: "Shop",
            TransactionDate: new DateOnly(2026, 4, 1),
            TotalAmount: 10.5m,
            TaxAmount: 1m,
            RawJson: raw,
            Lines: lines,
            Currency: "EUR");

        var doc = _normalizer.Normalize(ocr, @"C:\inbox\ticket.jpg", "abc123hash");

        Assert.Equal(@"C:\inbox\ticket.jpg", doc.FilePath);
        Assert.Equal("abc123hash", doc.FileHash);
        Assert.Equal("Shop", doc.MerchantName);
        Assert.Equal(new DateOnly(2026, 4, 1), doc.TransactionDate);
        Assert.Equal("EUR", doc.Currency);
        Assert.Equal(10.5m, doc.TotalAmount);
        Assert.Equal(1m, doc.TaxAmount);
        Assert.Equal(raw, doc.RawJson);
        Assert.Equal(ReceiptOcrStatuses.Success, doc.OcrStatus);
        Assert.Equal(100m, doc.Confidence);
        Assert.Single(doc.DocumentLines);
        var dl = doc.DocumentLines.First();
        Assert.Equal("Item A", dl.Description);
        Assert.Equal(2m, dl.Quantity);
        Assert.Equal(3m, dl.UnitPrice);
        Assert.Equal(6m, dl.Amount);
        Assert.Equal("EUR", dl.Currency);
    }

    [Fact]
    public void Normalize_partial_when_minimum_key_fields_missing_but_secondary_present()
    {
        var ocr = new OcrResult(
            MerchantName: null,
            TransactionDate: new DateOnly(2026, 1, 2),
            TotalAmount: null,
            TaxAmount: 0.5m,
            RawJson: "{}",
            Lines: Array.Empty<OcrLineItem>());

        var doc = _normalizer.Normalize(ocr, "/x/a.png", "h1");

        Assert.Equal(ReceiptOcrStatuses.Partial, doc.OcrStatus);
        Assert.True(doc.Confidence > 0 && doc.Confidence < 100);
        Assert.Null(doc.MerchantName);
        Assert.Null(doc.TotalAmount);
    }

    [Fact]
    public void Normalize_failed_when_no_structured_signals()
    {
        var ocr = new OcrResult(
            MerchantName: null,
            TransactionDate: null,
            TotalAmount: null,
            TaxAmount: null,
            RawJson: "{}",
            Lines: Array.Empty<OcrLineItem>());

        var doc = _normalizer.Normalize(ocr, "/x/b.pdf", "h2");

        Assert.Equal(ReceiptOcrStatuses.Failed, doc.OcrStatus);
        Assert.Equal(0m, doc.Confidence);
        Assert.Empty(doc.DocumentLines);
    }

    [Fact]
    public void Normalize_success_with_total_only_and_builds_lines_from_ocr()
    {
        var ocr = new OcrResult(
            MerchantName: null,
            TransactionDate: null,
            TotalAmount: 9.99m,
            TaxAmount: null,
            RawJson: "{}",
            Lines: new[]
            {
                new OcrLineItem("X", null, null, null),
                new OcrLineItem("Y", 2m, 1.5m, null),
            });

        var doc = _normalizer.Normalize(ocr, "/in/x.png", "h3");

        Assert.Equal(ReceiptOcrStatuses.Success, doc.OcrStatus);
        Assert.Equal(2, doc.DocumentLines.Count);
        var ordered = doc.DocumentLines.OrderBy(l => l.Description).ToList();
        Assert.Equal(0m, ordered[0].Amount);
        Assert.Equal(3m, ordered[1].Amount);
    }

    [Fact]
    public void Normalize_partial_when_only_line_items_without_merchant_or_total()
    {
        var ocr = new OcrResult(
            MerchantName: null,
            TransactionDate: null,
            TotalAmount: null,
            TaxAmount: null,
            RawJson: "{}",
            Lines: new[] { new OcrLineItem("Only line", 1m, 2m, 2m) });

        var doc = _normalizer.Normalize(ocr, "/a", "h4");

        Assert.Equal(ReceiptOcrStatuses.Partial, doc.OcrStatus);
        Assert.Single(doc.DocumentLines);
    }

    [Fact]
    public void Normalize_trims_merchant_and_currency()
    {
        var ocr = new OcrResult(
            MerchantName: "  ACME  ",
            TransactionDate: null,
            TotalAmount: 1m,
            TaxAmount: null,
            RawJson: "{}",
            Lines: Array.Empty<OcrLineItem>(),
            Currency: " USD ");

        var doc = _normalizer.Normalize(ocr, "/a", "h");

        Assert.Equal("ACME", doc.MerchantName);
        Assert.Equal("USD", doc.Currency);
    }
}
