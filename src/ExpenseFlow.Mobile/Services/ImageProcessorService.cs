using SkiaSharp;

namespace ExpenseFlow.Mobile.Services;

public sealed class ImageProcessorService
{
    public Task<Stream> ProcessAsync(Stream inputStream, SKRectI cropRect, CancellationToken ct = default) =>
        Task.Run(() => (Stream)ProcessCore(inputStream, cropRect), ct);

    private static MemoryStream ProcessCore(Stream inputStream, SKRectI cropRect)
    {
        using var decoded = SKBitmap.Decode(inputStream);
        if (decoded is null)
            throw new InvalidOperationException("No se pudo decodificar la imagen.");

        var bounds = new SKRectI(0, 0, decoded.Width, decoded.Height);
        cropRect = SKRectI.Intersect(cropRect, bounds);
        if (cropRect.Width < 1 || cropRect.Height < 1)
            throw new InvalidOperationException("El área de recorte no es válida.");

        using var cropped = new SKBitmap(cropRect.Width, cropRect.Height);
        if (!decoded.ExtractSubset(cropped, cropRect))
            throw new InvalidOperationException("No se pudo extraer el recorte.");

        var w = cropped.Width;
        var h = cropped.Height;
        var longSide = Math.Max(w, h);
        if (longSide > 1800)
        {
            var scale = 1800f / longSide;
            w = Math.Max(1, (int)Math.Round(w * scale));
            h = Math.Max(1, (int)Math.Round(h * scale));
        }

        using var scaled = cropped.Resize(new SKImageInfo(w, h), new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.None))
            ?? throw new InvalidOperationException("No se pudo escalar la imagen.");

        ReadOnlySpan<float> luminance =
        [
            0.299f, 0.587f, 0.114f, 0, 0,
            0.299f, 0.587f, 0.114f, 0, 0,
            0.299f, 0.587f, 0.114f, 0, 0,
            0, 0, 0, 1, 0,
        ];

        using var grayFilter = SKColorFilter.CreateColorMatrix(luminance);
        using var contrastFilter = SKColorFilter.CreateHighContrast(
            new SKHighContrastConfig(false, SKHighContrastConfigInvertStyle.NoInvert, 0.3f));
        using var composed = SKColorFilter.CreateCompose(contrastFilter, grayFilter);

        using var filtered = new SKBitmap(scaled.Width, scaled.Height, scaled.ColorType, scaled.AlphaType);
        using (var canvas = new SKCanvas(filtered))
        using (var paint = new SKPaint { ColorFilter = composed, IsAntialias = true })
        {
            canvas.Clear(SKColors.Black);
            canvas.DrawBitmap(scaled, 0, 0, paint);
        }

        using var image = SKImage.FromBitmap(filtered);
        using var encoded = image.Encode(SKEncodedImageFormat.Jpeg, 88);
        if (encoded is null)
            throw new InvalidOperationException("No se pudo codificar JPEG.");

        var output = new MemoryStream();
        encoded.SaveTo(output);
        output.Position = 0;
        return output;
    }
}
