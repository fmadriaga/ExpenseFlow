using ExpenseFlow.Api.Endpoints;
using ExpenseFlow.Application.Export;
using ExpenseFlow.Infrastructure;
using ExpenseFlow.Infrastructure.Configuration;
using ExpenseFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

ExpenseFlowConnectionStringValidator.EnsureConfigured(builder.Configuration);

builder.Services.AddPersistence(builder.Configuration, builder.Environment);
builder.Services.AddFileScanning(builder.Configuration);
builder.Services.AddFileStorage();
builder.Services.AddSingleton<ICsvExporter, DocumentCsvExporter>();

var app = builder.Build();

// En entorno "Testing" (p. ej. WebApplicationFactory) no migrar aquí: evita contención del
// __EFMigrationsLock de EF Core 9 con SQLite; los tests migran la BD antes de sembrar datos.
if (!app.Environment.IsEnvironment("Testing"))
{
    await using (var scope = app.Services.CreateAsyncScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ExpenseFlowDbContext>();
        await db.Database.MigrateAsync();
    }
}

app.MapGet("/", () => "ExpenseFlow API");
app.MapDocumentsEndpoints();
app.MapMembersEndpoints();

await app.RunAsync();

public partial class Program;
