using ExpenseFlow.Application.Options;
using ExpenseFlow.Infrastructure.Configuration;
using ExpenseFlow.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace ExpenseFlow.IntegrationTests;

/// <summary>
/// Fail-fast de opciones y cadena de conexión (TASK-008 / TASK-017).
/// </summary>
public sealed class ConfigurationValidationTests
{
    private static Dictionary<string, string?> ValidOptionsDictionary() =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["ConnectionStrings:ExpenseFlow"] = "Data Source=:memory:",
            ["Storage:Inbox"] = "/tmp/expenseflow/inbox",
            ["Storage:Processed"] = "/tmp/expenseflow/processed",
            ["Storage:Error"] = "/tmp/expenseflow/error",
            ["AzureDocumentIntelligence:Endpoint"] = "https://placeholder.invalid",
            ["AzureDocumentIntelligence:ApiKey"] = "local-test-key",
            ["AzureDocumentIntelligence:MaxRetries"] = "3",
            ["AzureDocumentIntelligence:BaseDelaySeconds"] = "1",
            ["Worker:IntervalSeconds"] = "60",
        };

    private static void RegisterValidatedOptions(IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<WorkerOptions>()
            .Bind(configuration.GetSection(WorkerOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services
            .AddOptions<StorageOptions>()
            .Bind(configuration.GetSection(StorageOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services
            .AddOptions<AzureDocumentIntelligenceOptions>()
            .Bind(configuration.GetSection(AzureDocumentIntelligenceOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<AzureDocumentIntelligenceOptions>, AzureDocumentIntelligenceOptionsValidator>();
    }

    private static async Task AssertOptionsFailOnStartAsync(
        Action<Dictionary<string, string?>> removeOrOverride,
        string messageFragment)
    {
        var data = ValidOptionsDictionary();
        removeOrOverride(data);
        var config = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        var host = new HostBuilder()
            .ConfigureServices(
                (context, services) =>
                {
                    RegisterValidatedOptions(services, config);
                })
            .Build();
        var ex = await Assert.ThrowsAsync<OptionsValidationException>(() => host.StartAsync());
        Assert.Contains(messageFragment, ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public Task Missing_Storage_Inbox_fails_with_clear_message() =>
        AssertOptionsFailOnStartAsync(
            d => d["Storage:Inbox"] = string.Empty,
            "Storage");

    [Fact]
    public Task Missing_Storage_Processed_fails_with_clear_message() =>
        AssertOptionsFailOnStartAsync(
            d => d["Storage:Processed"] = string.Empty,
            "Storage");

    [Fact]
    public Task Missing_Storage_Error_fails_with_clear_message() =>
        AssertOptionsFailOnStartAsync(
            d => d["Storage:Error"] = string.Empty,
            "Storage");

    [Fact]
    public Task Missing_Azure_Endpoint_fails_with_clear_message() =>
        AssertOptionsFailOnStartAsync(
            d => d["AzureDocumentIntelligence:Endpoint"] = string.Empty,
            "AzureDocumentIntelligence");

    [Fact]
    public Task Missing_Azure_ApiKey_fails_with_clear_message() =>
        AssertOptionsFailOnStartAsync(
            d => d["AzureDocumentIntelligence:ApiKey"] = string.Empty,
            "AzureDocumentIntelligence");

    [Fact]
    public Task Worker_IntervalSeconds_zero_fails_with_clear_message() =>
        AssertOptionsFailOnStartAsync(
            d => d["Worker:IntervalSeconds"] = "0",
            "Worker");

    [Fact]
    public void Missing_connection_string_fails_with_InvalidOperationException()
    {
        var config = new ConfigurationBuilder().Build();
        var ex = Assert.Throws<InvalidOperationException>(
            () => ExpenseFlowConnectionStringValidator.EnsureConfigured(config));
        Assert.Contains("ConnectionStrings", ex.Message, StringComparison.Ordinal);
    }
}
