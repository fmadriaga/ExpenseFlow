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

public sealed record DocumentLine(
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("quantity")] decimal? Quantity,
    [property: JsonPropertyName("unitPrice")] decimal? UnitPrice,
    [property: JsonPropertyName("amount")] decimal Amount,
    [property: JsonPropertyName("currency")] string? Currency);

public sealed record DocumentDetail(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("merchantName")] string? MerchantName,
    [property: JsonPropertyName("transactionDate")] DateOnly? TransactionDate,
    [property: JsonPropertyName("totalAmount")] decimal? TotalAmount,
    [property: JsonPropertyName("currency")] string? Currency,
    [property: JsonPropertyName("category")] string? Category,
    [property: JsonPropertyName("ocrStatus")] string? OcrStatus,
    [property: JsonPropertyName("confidence")] decimal Confidence,
    [property: JsonPropertyName("errorMessage")] string? ErrorMessage,
    [property: JsonPropertyName("createdAt")] DateTimeOffset CreatedAt,
    [property: JsonPropertyName("lines")] IReadOnlyList<DocumentLine> Lines);

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

    public async Task<DocumentDetail?> GetDetailAsync(
        int id,
        int familyId = 1,
        CancellationToken ct = default)
    {
        var requestUri = $"documents/{id}?familyId={familyId}";
        try
        {
            using var response = await _httpClient.GetAsync(requestUri, ct);
            if (!response.IsSuccessStatusCode)
            {
                LogError($"API error calling '{requestUri}': {(int)response.StatusCode}");
                return null;
            }
            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            return await JsonSerializer.DeserializeAsync<DocumentDetail>(stream, JsonOptions, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            LogError($"Network/deserialization error calling '{requestUri}': {ex.Message}");
            return null;
        }
    }

    public async Task<bool> PatchDocumentAsync(
        int id,
        string? merchantName,
        DateOnly? transactionDate,
        decimal? totalAmount,
        int familyId = 1,
        CancellationToken ct = default)
    {
        var requestUri = $"documents/{id}?familyId={familyId}";
        var body = new
        {
            merchantName,
            transactionDate,
            totalAmount,
        };
        try
        {
            var json = JsonSerializer.Serialize(body, JsonOptions);
            using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            using var response = await _httpClient.PatchAsync(requestUri, content, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            LogError($"Patch error calling '{requestUri}': {ex.Message}");
            return false;
        }
    }

    public async Task<bool> ReprocessAsync(
        int id,
        int familyId = 1,
        CancellationToken ct = default)
    {
        var requestUri = $"documents/{id}/reprocess?familyId={familyId}";
        try
        {
            using var response = await _httpClient.PostAsync(requestUri, null, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            LogError($"Reprocess error calling '{requestUri}': {ex.Message}");
            return false;
        }
    }

    private static void LogError(string message)
    {
        Debug.WriteLine(message);
        Console.Error.WriteLine(message);
    }
}
