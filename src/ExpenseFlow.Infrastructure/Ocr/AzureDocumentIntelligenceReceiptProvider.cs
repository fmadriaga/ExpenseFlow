using Azure;
using Azure.AI.DocumentIntelligence;
using ExpenseFlow.Application.Abstractions;
using ExpenseFlow.Application.Ocr;
using ExpenseFlow.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace ExpenseFlow.Infrastructure.Ocr;

public sealed class AzureDocumentIntelligenceReceiptProvider : IReceiptOcrProvider
{
    private const string PrebuiltReceiptModelId = "prebuilt-receipt";

    private readonly DocumentIntelligenceClient _client;
    private readonly IOptions<AzureDocumentIntelligenceOptions> _options;
    private readonly ILogger<AzureDocumentIntelligenceReceiptProvider> _logger;

    public AzureDocumentIntelligenceReceiptProvider(
        IOptions<AzureDocumentIntelligenceOptions> options,
        ILogger<AzureDocumentIntelligenceReceiptProvider> logger)
    {
        var config = options.Value;
        if (string.IsNullOrWhiteSpace(config.Endpoint))
        {
            throw new InvalidOperationException(
                $"{AzureDocumentIntelligenceOptions.SectionName}:Endpoint is required.");
        }

        if (string.IsNullOrWhiteSpace(config.ApiKey))
        {
            throw new InvalidOperationException(
                $"{AzureDocumentIntelligenceOptions.SectionName}:ApiKey is required.");
        }

        _client = new DocumentIntelligenceClient(
            new Uri(config.Endpoint),
            new AzureKeyCredential(config.ApiKey));
        _options = options;
        _logger = logger;
    }

    public async Task<OcrResult> AnalyzeReceiptAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path is required.", nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException(
                $"OCR source file not found: {filePath}", filePath);
        }

        var ocrOptions = _options.Value;
        return await OcrAnalysisRetryHelper.ExecuteWithRetryAsync(
            async token =>
            {
                await using var stream = new FileStream(
                    filePath,
                    new FileStreamOptions
                    {
                        Mode = FileMode.Open,
                        Access = FileAccess.Read,
                        Share = FileShare.Read,
                        BufferSize = 1024 * 8,
                        Options = FileOptions.Asynchronous | FileOptions.SequentialScan,
                    });
                var source = BinaryData.FromStream(stream);
                var operation = await _client
                    .AnalyzeDocumentAsync(
                        WaitUntil.Completed,
                        PrebuiltReceiptModelId,
                        source,
                        token)
                    .ConfigureAwait(false);
                var rawJson = JsonSerializer.Serialize(operation.Value);
                return AzureReceiptResultMapper.MapFromRawJson(rawJson);
            },
            ocrOptions.MaxRetries,
            ocrOptions.BaseDelaySeconds,
            _logger,
            filePath,
            cancellationToken)
            .ConfigureAwait(false);
    }
}
