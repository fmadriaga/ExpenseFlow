using ExpenseFlow.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
