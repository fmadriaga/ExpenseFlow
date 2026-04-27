using ExpenseFlow.Application.Options;
using Microsoft.Extensions.Options;

namespace ExpenseFlow.Infrastructure.Configuration;

/// <summary>
/// Valida que <see cref="AzureDocumentIntelligenceOptions.Endpoint"/> sea una URI absoluta HTTP/HTTPS.
/// </summary>
public sealed class AzureDocumentIntelligenceOptionsValidator : IValidateOptions<AzureDocumentIntelligenceOptions>
{
    public ValidateOptionsResult Validate(string? name, AzureDocumentIntelligenceOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var ep = options.Endpoint?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(ep))
        {
            return ValidateOptionsResult.Fail("AzureDocumentIntelligence:Endpoint is required.");
        }

        if (!Uri.TryCreate(ep, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return ValidateOptionsResult.Fail(
                $"AzureDocumentIntelligence:Endpoint must be an absolute HTTP or HTTPS URI. Received: \"{ep}\".");
        }

        return ValidateOptionsResult.Success;
    }
}
