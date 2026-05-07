using ExpenseFlow.Application.Options;
using ExpenseFlow.Infrastructure.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace ExpenseFlow.Api.Endpoints;

public static class UploadEndpoints
{
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".pdf"];

    public static IEndpointRouteBuilder MapUploadEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost(
            "/upload/ticket",
            async Task<IResult> (
                IFormFile file,
                IOptions<StorageOptions> storageOptions,
                IWebHostEnvironment env,
                CancellationToken cancellationToken) =>
            {
                if (file.Length == 0)
                    return Results.BadRequest(new { error = "El archivo está vacío." });

                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!AllowedExtensions.Contains(ext))
                    return Results.BadRequest(new { error = $"Extensión no permitida. Usar: {string.Join(", ", AllowedExtensions)}" });

                var inboxPath = ContentRootPathResolver.Resolve(
                    env.ContentRootPath,
                    storageOptions.Value.Inbox);

                Directory.CreateDirectory(inboxPath);

                var fileName = $"ticket_{DateTime.UtcNow:yyyyMMdd_HHmmss_fff}{ext}";
                var fullPath = Path.Combine(inboxPath, fileName);

                await using var stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
                await file.CopyToAsync(stream, cancellationToken);

                return Results.Created($"/upload/ticket/{fileName}", new
                {
                    fileName,
                    message = "Ticket recibido. Será procesado en el próximo ciclo del Worker."
                });
            })
            .WithTags("Upload")
            .WithSummary("Upload ticket image")
            .WithDescription("Recibe una imagen de ticket (jpg, jpeg, png, pdf) y la guarda en la carpeta inbox para procesamiento por el Worker.")
            .DisableAntiforgery();

        return app;
    }
}
