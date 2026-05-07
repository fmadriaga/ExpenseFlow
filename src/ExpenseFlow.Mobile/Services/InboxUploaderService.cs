using Microsoft.Extensions.Configuration;

namespace ExpenseFlow.Mobile.Services;

public sealed class InboxUploaderService
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public InboxUploaderService(IConfiguration configuration, HttpClient httpClient)
    {
        _configuration = configuration;
        _httpClient = httpClient;
    }

    public async Task<string> UploadAsync(Stream photoStream, CancellationToken ct = default)
    {
#if WINDOWS
        return await SaveLocalAsync(photoStream, ct);
#else
        return await PostToApiAsync(photoStream, ct);
#endif
    }

    // Windows: escribe directo a la carpeta inbox (Drive/OneDrive la sincroniza)
    private async Task<string> SaveLocalAsync(Stream photoStream, CancellationToken ct)
    {
        var inboxLocalPath = _configuration["ExpenseFlow:InboxLocalPath"];
        if (string.IsNullOrWhiteSpace(inboxLocalPath))
            throw new InvalidOperationException(
                "Configuration key 'ExpenseFlow:InboxLocalPath' es requerida en Windows.");

        Directory.CreateDirectory(inboxLocalPath);
        var fileName = $"ticket_{DateTime.UtcNow:yyyyMMdd_HHmmss_fff}.jpg";
        var fullPath = Path.Combine(inboxLocalPath, fileName);

        await using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);
        await photoStream.CopyToAsync(fileStream, ct);
        return fullPath;
    }

    // Android/iOS: POST multipart a la API, que guarda en inbox
    private async Task<string> PostToApiAsync(Stream photoStream, CancellationToken ct)
    {
        var fileName = $"ticket_{DateTime.UtcNow:yyyyMMdd_HHmmss_fff}.jpg";

        using var content = new MultipartFormDataContent();
        using var streamContent = new StreamContent(photoStream);
        streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        content.Add(streamContent, "file", fileName);

        var response = await _httpClient.PostAsync("upload/ticket", content, ct);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException($"Error al subir ticket: {response.StatusCode} — {error}");
        }

        return fileName;
    }
}
