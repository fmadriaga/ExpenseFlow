using ExpenseFlow.Application.Ocr;
using System.Globalization;
using System.Text.Json;

namespace ExpenseFlow.Infrastructure.Ocr;

public static class AzureReceiptResultMapper
{
    public static OcrResult MapFromRawJson(string rawJson)
    {
        using var document = JsonDocument.Parse(rawJson);
        var root = document.RootElement;
        var lines = new List<OcrLineItem>();

        if (!TryGetField(root, "MerchantName", out var merchantField))
        {
            merchantField = default;
        }

        if (!TryGetField(root, "TransactionDate", out var dateField))
        {
            dateField = default;
        }

        if (!TryGetField(root, "Total", out var totalField))
        {
            totalField = default;
        }

        if (!TryGetField(root, "TotalTax", out var taxField))
        {
            taxField = default;
        }

        if (TryGetField(root, "Items", out var itemsField) &&
            TryGetValueArray(itemsField, out var items))
        {
            foreach (var item in items.EnumerateArray())
            {
                if (!TryGetValueObject(item, out var valueObject))
                {
                    continue;
                }

                lines.Add(new OcrLineItem(
                    Description: GetStringField(valueObject, "Description"),
                    Quantity: GetDecimalField(valueObject, "Quantity"),
                    UnitPrice: GetDecimalField(valueObject, "Price"),
                    TotalPrice: GetDecimalField(valueObject, "TotalPrice")));
            }
        }

        return new OcrResult(
            MerchantName: GetStringValue(merchantField),
            TransactionDate: GetDateValue(dateField),
            TotalAmount: GetDecimalValue(totalField),
            TaxAmount: GetDecimalValue(taxField),
            RawJson: rawJson,
            Lines: lines,
            Currency: GetCurrencyCode(totalField));
    }

    private static bool TryGetField(JsonElement root, string fieldName, out JsonElement fieldElement)
    {
        fieldElement = default;
        if (!root.TryGetProperty("documents", out var docs) ||
            docs.ValueKind != JsonValueKind.Array ||
            docs.GetArrayLength() == 0)
        {
            return false;
        }

        var first = docs[0];
        if (!first.TryGetProperty("fields", out var fields) ||
            fields.ValueKind != JsonValueKind.Object ||
            !fields.TryGetProperty(fieldName, out fieldElement))
        {
            return false;
        }

        return true;
    }

    private static string? GetStringField(JsonElement valueObject, string fieldName)
    {
        if (!valueObject.TryGetProperty(fieldName, out var field))
        {
            return null;
        }

        return GetStringValue(field);
    }

    private static decimal? GetDecimalField(JsonElement valueObject, string fieldName)
    {
        if (!valueObject.TryGetProperty(fieldName, out var field))
        {
            return null;
        }

        return GetDecimalValue(field);
    }

    private static string? GetStringValue(JsonElement field)
    {
        if (field.ValueKind == JsonValueKind.Undefined)
        {
            return null;
        }

        if (field.TryGetProperty("valueString", out var valueString) &&
            valueString.ValueKind == JsonValueKind.String)
        {
            return valueString.GetString();
        }

        if (field.TryGetProperty("content", out var content) &&
            content.ValueKind == JsonValueKind.String)
        {
            return content.GetString();
        }

        return null;
    }

    private static DateOnly? GetDateValue(JsonElement field)
    {
        if (field.ValueKind == JsonValueKind.Undefined)
        {
            return null;
        }

        if (field.TryGetProperty("valueDate", out var valueDate) &&
            valueDate.ValueKind == JsonValueKind.String &&
            DateOnly.TryParse(valueDate.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return date;
        }

        return null;
    }

    private static string? GetCurrencyCode(JsonElement field)
    {
        if (field.ValueKind == JsonValueKind.Undefined)
        {
            return null;
        }

        if (field.TryGetProperty("valueCurrency", out var valueCurrency) &&
            valueCurrency.TryGetProperty("currencyCode", out var code) &&
            code.ValueKind == JsonValueKind.String)
        {
            return code.GetString();
        }

        return null;
    }

    private static decimal? GetDecimalValue(JsonElement field)
    {
        if (field.ValueKind == JsonValueKind.Undefined)
        {
            return null;
        }

        if (field.TryGetProperty("valueCurrency", out var valueCurrency) &&
            valueCurrency.TryGetProperty("amount", out var amountProp) &&
            TryReadDecimal(amountProp, out var amount))
        {
            return amount;
        }

        if (field.TryGetProperty("valueNumber", out var valueNumber) &&
            TryReadDecimal(valueNumber, out var number))
        {
            return number;
        }

        return null;
    }

    private static bool TryReadDecimal(JsonElement value, out decimal result)
    {
        if (value.ValueKind == JsonValueKind.Number &&
            value.TryGetDecimal(out result))
        {
            return true;
        }

        if (value.ValueKind == JsonValueKind.String &&
            decimal.TryParse(
                value.GetString(),
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out result))
        {
            return true;
        }

        result = default;
        return false;
    }

    private static bool TryGetValueArray(JsonElement field, out JsonElement array)
    {
        array = default;
        return field.TryGetProperty("valueArray", out array) &&
               array.ValueKind == JsonValueKind.Array;
    }

    private static bool TryGetValueObject(JsonElement field, out JsonElement valueObject)
    {
        valueObject = default;
        return field.TryGetProperty("valueObject", out valueObject) &&
               valueObject.ValueKind == JsonValueKind.Object;
    }
}
