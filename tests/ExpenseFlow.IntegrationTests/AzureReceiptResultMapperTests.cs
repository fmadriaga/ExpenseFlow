using ExpenseFlow.Infrastructure.Ocr;
using Xunit;

namespace ExpenseFlow.IntegrationTests;

public class AzureReceiptResultMapperTests
{
    [Fact]
    public void MapFromRawJson_maps_receipt_fields_and_lines()
    {
        const string rawJson = """
            {
              "documents": [
                {
                  "fields": {
                    "MerchantName": { "valueString": "Contoso Market" },
                    "TransactionDate": { "valueDate": "2026-04-26" },
                    "Total": { "valueCurrency": { "amount": 19.99, "currencyCode": "USD" } },
                    "TotalTax": { "valueCurrency": { "amount": 1.23, "currencyCode": "USD" } },
                    "Items": {
                      "valueArray": [
                        {
                          "valueObject": {
                            "Description": { "valueString": "Milk" },
                            "Quantity": { "valueNumber": 2 },
                            "Price": { "valueCurrency": { "amount": 3.50 } },
                            "TotalPrice": { "valueCurrency": { "amount": 7.00 } }
                          }
                        }
                      ]
                    }
                  }
                }
              ]
            }
            """;

        var result = AzureReceiptResultMapper.MapFromRawJson(rawJson);

        Assert.Equal("Contoso Market", result.MerchantName);
        Assert.Equal(new DateOnly(2026, 4, 26), result.TransactionDate);
        Assert.Equal(19.99m, result.TotalAmount);
        Assert.Equal(1.23m, result.TaxAmount);
        Assert.Equal(rawJson, result.RawJson);
        Assert.Single(result.Lines);
        var line = result.Lines[0];
        Assert.Equal("Milk", line.Description);
        Assert.Equal(2m, line.Quantity);
        Assert.Equal(3.50m, line.UnitPrice);
        Assert.Equal(7.00m, line.TotalPrice);
    }
}
