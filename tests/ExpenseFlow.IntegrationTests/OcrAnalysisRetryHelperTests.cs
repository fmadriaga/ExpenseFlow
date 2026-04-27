using Azure;
using ExpenseFlow.Infrastructure.Ocr;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ExpenseFlow.IntegrationTests;

public sealed class OcrAnalysisRetryHelperTests
{
    [Fact]
    public async Task Transient_failures_are_retried_until_success()
    {
        var calls = 0;
        var result = await OcrAnalysisRetryHelper.ExecuteWithRetryAsync(
            _ =>
            {
                calls++;
                if (calls < 3)
                {
                    return Task.FromException<int>(
                        new RequestFailedException(429, "throttle", "throttle", null));
                }

                return Task.FromResult(42);
            },
            maxAdditionalRetries: 3,
            baseDelaySeconds: 0.01,
            NullLogger.Instance,
            "inbox/ticket.png",
            CancellationToken.None);
        Assert.Equal(42, result);
        Assert.Equal(3, calls);
    }

    [Fact]
    public async Task Non_transient_fails_on_first_try_without_extra_calls()
    {
        var calls = 0;
        var ex = await Assert.ThrowsAsync<RequestFailedException>(
            () => OcrAnalysisRetryHelper.ExecuteWithRetryAsync(
                _ =>
                {
                    calls++;
                    return Task.FromException<int>(
                        new RequestFailedException(400, "bad request", "bad", null));
                },
                maxAdditionalRetries: 3,
                baseDelaySeconds: 0.01,
                NullLogger.Instance,
                "inbox/bad.png",
                CancellationToken.None));
        Assert.Equal(400, ex.Status);
        Assert.Equal(1, calls);
    }

    [Fact]
    public void IsTransientOcrFailure_429_true_400_false()
    {
        Assert.True(
            OcrAnalysisRetryHelper.IsTransientOcrFailure(
                new RequestFailedException(429, "x", "x", null),
                CancellationToken.None));
        Assert.False(
            OcrAnalysisRetryHelper.IsTransientOcrFailure(
                new RequestFailedException(400, "x", "x", null),
                CancellationToken.None));
    }
}
