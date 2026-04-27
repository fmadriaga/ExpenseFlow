using ExpenseFlow.Application.Abstractions;
using ExpenseFlow.Application.Options;
using ExpenseFlow.Application.Services;
using ExpenseFlow.Infrastructure.Data;
using ExpenseFlow.Infrastructure.Ocr;
using ExpenseFlow.Infrastructure.Options;
using ExpenseFlow.Infrastructure.Scanning;
using ExpenseFlow.Infrastructure.Storage;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace ExpenseFlow.Infrastructure;

public static class DependencyInjection
{
    private const string ConnectionStringName = "ExpenseFlow";

    /// <summary>
    /// Registra <see cref="ExpenseFlowDbContext"/> con SQLite. La base por defecto
    /// es <c>data/expenseflow.db</c> en la raíz del repositorio (dos niveles
    /// por encima del <c>ContentRoot</c> del host).
    /// </summary>
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment hostEnvironment)
    {
        var connectionString = ResolveConnectionString(
            configuration,
            hostEnvironment);
        return services.AddDbContext<ExpenseFlowDbContext>(options =>
            options.UseSqlite(connectionString));
    }

    /// <summary>
    /// Registra <see cref="IFileScanner"/>, <see cref="IOptions{StorageOptions}"/>
    /// y ajuste de rutas a absolutas respecto al <c>ContentRoot</c> del host.
    /// </summary>
    public static IServiceCollection AddFileScanning(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<StorageOptions>()
            .Bind(
                configuration.GetSection(StorageOptions.SectionName));
        services.AddSingleton<IPostConfigureOptions<StorageOptions>, PostConfigureStoragePaths>();
        services.AddScoped<IFileScanner, FileScanner>();
        return services;
    }

    /// <summary>
    /// Registra <see cref="IFileMover"/> para mover archivos a <c>processed</c> o <c>error</c>.
    /// Requiere que <see cref="StorageOptions"/> ya esté registrado (p. ej. vía <see cref="AddFileScanning"/>).
    /// </summary>
    public static IServiceCollection AddFileStorage(this IServiceCollection services)
    {
        services.AddScoped<IFileMover, FileMover>();
        return services;
    }

    /// <summary>
    /// Registra proveedores OCR y sus opciones tipadas.
    /// </summary>
    public static IServiceCollection AddOcrProviders(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<AzureDocumentIntelligenceOptions>()
            .Bind(configuration.GetSection(AzureDocumentIntelligenceOptions.SectionName));
        services.AddScoped<IReceiptOcrProvider, AzureDocumentIntelligenceReceiptProvider>();
        return services;
    }

    /// <summary>
    /// Registra el normalizador de recibos (mapeo de OCR a entidad <c>Document</c>).
    /// </summary>
    public static IServiceCollection AddReceiptNormalization(this IServiceCollection services)
    {
        services.AddSingleton<IReceiptNormalizer, ReceiptNormalizer>();
        return services;
    }

    private static string ResolveConnectionString(
        IConfiguration configuration,
        IHostEnvironment hostEnvironment)
    {
        var custom = configuration.GetConnectionString(ConnectionStringName);
        if (string.IsNullOrWhiteSpace(custom))
        {
            return BuildConnectionStringForPath(ResolveDefaultDatabasePath(hostEnvironment));
        }

        if (IsSqliteKeyValueString(custom))
        {
            return custom.Trim();
        }

        var dataSource = custom.Trim().Trim('"');
        var dataSourcePath = Path.GetFullPath(
            Path.IsPathRooted(dataSource)
                ? dataSource
                : Path.Combine(hostEnvironment.ContentRootPath, dataSource));
        return BuildConnectionStringForPath(dataSourcePath);
    }

    private static string ResolveDefaultDatabasePath(IHostEnvironment hostEnvironment) =>
        Path.GetFullPath(
            Path.Combine(
                hostEnvironment.ContentRootPath,
                "..",
                "..",
                "data",
                "expenseflow.db"));

    private static bool IsSqliteKeyValueString(string value) =>
        value.Contains("Data Source", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("DataSource", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("Mode", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("Cache", StringComparison.OrdinalIgnoreCase);

    private static string BuildConnectionStringForPath(string absoluteDataSourcePath)
    {
        var directory = Path.GetDirectoryName(absoluteDataSourcePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        return new SqliteConnectionStringBuilder { DataSource = absoluteDataSourcePath }
            .ConnectionString;
    }
}
