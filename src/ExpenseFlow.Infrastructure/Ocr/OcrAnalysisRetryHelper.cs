using Azure;
using Microsoft.Extensions.Logging;

namespace ExpenseFlow.Infrastructure.Ocr;

/// <summary>
/// Reintentos con backoff exponencial para fallos transitorios del proveedor OCR (sin exponer Polly al resto de la solución).
/// </summary>
public static class OcrAnalysisRetryHelper
{
    private const double MaxBackoffSeconds = 120.0;

    public static bool IsTransientOcrFailure(Exception ex, CancellationToken cancellationToken)
    {
        for (var e = ex; e is not null; e = e.InnerException)
        {
            if (e is TaskCanceledException tce)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return false;
                }

                if (tce.CancellationToken.IsCancellationRequested)
                {
                    return false;
                }

                return true;
            }

            if (e is OperationCanceledException)
            {
                return false;
            }

            if (e is HttpRequestException)
            {
                return true;
            }

            if (e is RequestFailedException r)
            {
                var code = (int)r.Status;
                if (code == 429 || code == 503 || code == 408)
                {
                    return true;
                }

                if (code >= 500 && code < 600)
                {
                    return true;
                }

                return false;
            }

            if (e is IOException)
            {
                return true;
            }
        }

        return false;
    }

    public static async Task<T> ExecuteWithRetryAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        int maxAdditionalRetries,
        double baseDelaySeconds,
        ILogger logger,
        string filePath,
        CancellationToken cancellationToken)
    {
        if (maxAdditionalRetries < 0)
        {
            maxAdditionalRetries = 0;
        }

        if (baseDelaySeconds < 0.01)
        {
            baseDelaySeconds = 0.01;
        }

        var maxTotal = 1 + maxAdditionalRetries;
        for (var attempt = 0; attempt < maxTotal; attempt++)
        {
            try
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException(cancellationToken);
                }

                return await operation(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }

                if (!IsTransientOcrFailure(ex, cancellationToken) || attempt >= maxTotal - 1)
                {
                    throw;
                }

                var delaySeconds = Math.Min(MaxBackoffSeconds, baseDelaySeconds * Math.Pow(2, attempt));
                var delay = TimeSpan.FromSeconds(delaySeconds);
                logger.LogWarning(
                    ex,
                    "OCR transitory failure; attempt {Attempt} of {MaxAttempts} will retry after {DelaySeconds:F1}s. File: {FilePath}, Reason: {Reason}",
                    attempt + 1,
                    maxTotal,
                    delay.TotalSeconds,
                    filePath,
                    ex.Message);
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }

        throw new InvalidOperationException("OCR retry loop exited without result.");
    }
}
