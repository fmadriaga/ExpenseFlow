using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ExpenseFlow.Mobile.Services;

public sealed record DocumentSummary(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("merchantName")] string? MerchantName,
    [property: JsonPropertyName("transactionDate")] DateOnly? TransactionDate,
    [property: JsonPropertyName("totalAmount")] decimal? TotalAmount,
    [property: JsonPropertyName("currency")] string? Currency,
    [property: JsonPropertyName("ocrStatus")] string? OcrStatus,
    [property: JsonPropertyName("createdAt")] DateTimeOffset CreatedAt);

public sealed class ExpenseFlowApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _httpClient;

    public ExpenseFlowApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<DocumentSummary>> GetRecentAsync(
        int limit = 20,
        int familyId = 1,
        CancellationToken ct = default)
    {
        var requestUri = $"documents/recent?limit={limit}&familyId={familyId}";

        try
        {
            using var response = await _httpClient.GetAsync(requestUri, ct);
            if (!response.IsSuccessStatusCode)
            {
                LogError($"API error calling '{requestUri}': {(int)response.StatusCode} {response.ReasonPhrase}");
                return [];
            }

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            var items = await JsonSerializer.DeserializeAsync<List<DocumentSummary>>(stream, JsonOptions, ct);
            return items ?? [];
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            LogError($"Network/deserialization error calling '{requestUri}': {ex.Message}");
            return [];
        }
    }

    private static void LogError(string message)
    {
        Debug.WriteLine(message);
        Console.Error.WriteLine(message);
    }
}
