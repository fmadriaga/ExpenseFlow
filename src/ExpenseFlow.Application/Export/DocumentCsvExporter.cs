using System.Globalization;
using System.Text;

namespace ExpenseFlow.Application.Export;

public sealed class DocumentCsvExporter : ICsvExporter
{
    private const string Crlf = "\r\n";

    public async Task WriteDocumentExportAsync(
        IAsyncEnumerable<DocumentExportRow> rows,
        TextWriter writer,
        char delimiter,
        CancellationToken cancellationToken = default)
    {
        await writer.WriteAsync(FormatHeader(delimiter) + Crlf).ConfigureAwait(false);
        await foreach (var row in rows.WithCancellation(cancellationToken))
        {
            await writer.WriteAsync(FormatRow(row, delimiter) + Crlf).ConfigureAwait(false);
        }
    }

    internal static string FormatHeader(char delimiter) =>
        string.Join(
            delimiter,
            "Id",
            "FilePath",
            "MerchantName",
            "TransactionDate",
            "TotalAmount",
            "TaxAmount",
            "Currency",
            "Category",
            "OcrStatus",
            "CreatedAt");

    internal static string FormatRow(DocumentExportRow row, char delimiter)
    {
        var inv = CultureInfo.InvariantCulture;
        return string.Join(
            delimiter,
            row.Id.ToString(inv),
            EscapeField(row.FilePath, delimiter),
            EscapeField(row.MerchantName, delimiter),
            row.TransactionDate?.ToString("yyyy-MM-dd", inv) ?? string.Empty,
            row.TotalAmount?.ToString(inv) ?? string.Empty,
            row.TaxAmount?.ToString(inv) ?? string.Empty,
            EscapeField(row.Currency, delimiter),
            EscapeField(row.Category, delimiter),
            EscapeField(row.OcrStatus, delimiter),
            row.CreatedAt.ToString("O", inv));
    }

    internal static string EscapeField(string? value, char delimiter)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var mustQuote = value.Contains(delimiter, StringComparison.Ordinal)
                        || value.Contains('"')
                        || value.Contains('\r')
                        || value.Contains('\n');
        if (!mustQuote)
        {
            return value;
        }

        return new StringBuilder().Append('"').Append(value.Replace("\"", "\"\"", StringComparison.Ordinal))
            .Append('"')
            .ToString();
    }
}
