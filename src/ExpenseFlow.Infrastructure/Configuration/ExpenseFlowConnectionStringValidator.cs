using Microsoft.Extensions.Configuration;

namespace ExpenseFlow.Infrastructure.Configuration;

/// <summary>
/// Validación temprana de la cadena SQLite antes de registrar EF Core.
/// </summary>
public static class ExpenseFlowConnectionStringValidator
{
    public const string ConnectionStringName = "ExpenseFlow";

    /// <summary>
    /// Comprueba que <c>ConnectionStrings:ExpenseFlow</c> exista y no esté vacía
    /// (tras el merge de configuración: appsettings, variables de entorno, User Secrets).
    /// </summary>
    /// <exception cref="InvalidOperationException">Si la cadena no está configurada.</exception>
    public static void EnsureConfigured(IConfiguration configuration)
    {
        var value = configuration.GetConnectionString(ConnectionStringName);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                "Falta la cadena de conexión obligatoria. Configure ConnectionStrings:ExpenseFlow " +
                "en appsettings.json, en User Secrets o con la variable de entorno ConnectionStrings__ExpenseFlow. " +
                "Ejemplo (ruta relativa al ContentRoot del Worker): ../../data/expenseflow.db");
        }
    }
}
