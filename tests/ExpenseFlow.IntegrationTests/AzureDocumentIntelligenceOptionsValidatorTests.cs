using ExpenseFlow.Application.Options;
using ExpenseFlow.Infrastructure.Configuration;
using Xunit;

namespace ExpenseFlow.IntegrationTests;

public sealed class AzureDocumentIntelligenceOptionsValidatorTests
{
    private readonly AzureDocumentIntelligenceOptionsValidator _sut = new();

    [Theory]
    [InlineData("https://example.cognitiveservices.azure.com/")]
    [InlineData("https://placeholder.invalid")]
    [InlineData("http://localhost:8080/")]
    public void Valid_http_https_uris_succeed(string endpoint)
    {
        var r = _sut.Validate(
            null,
            new AzureDocumentIntelligenceOptions
            {
                Endpoint = endpoint,
                ApiKey = "k",
            });
        Assert.True(r.Succeeded);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_or_whitespace_endpoint_fails(string endpoint)
    {
        var r = _sut.Validate(
            null,
            new AzureDocumentIntelligenceOptions
            {
                Endpoint = endpoint,
                ApiKey = "k",
            });
        Assert.True(r.Failed);
        Assert.Contains("Endpoint is required", r.FailureMessage, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("not-a-uri")]
    [InlineData("ftp://files.example/blob")]
    [InlineData("/relative/path")]
    public void Non_http_absolute_uri_fails(string endpoint)
    {
        var r = _sut.Validate(
            null,
            new AzureDocumentIntelligenceOptions
            {
                Endpoint = endpoint,
                ApiKey = "k",
            });
        Assert.True(r.Failed);
        Assert.Contains("HTTP or HTTPS", r.FailureMessage, StringComparison.Ordinal);
    }
}
