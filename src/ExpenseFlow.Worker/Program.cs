using ExpenseFlow.Application.Options;
using ExpenseFlow.Infrastructure;
using ExpenseFlow.Infrastructure.Configuration;
using ExpenseFlow.Infrastructure.Data;
using ExpenseFlow.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);

ExpenseFlowConnectionStringValidator.EnsureConfigured(builder.Configuration);

builder.Services
    .AddOptions<WorkerOptions>()
    .Bind(builder.Configuration.GetSection(WorkerOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddPersistence(builder.Configuration, builder.Environment);
builder.Services.AddFileScanning(builder.Configuration);
builder.Services.AddFileStorage();
builder.Services.AddOcrProviders(builder.Configuration);
builder.Services.AddReceiptNormalization();
builder.Services.AddCategorization(builder.Configuration);
builder.Services.AddHostedService<ExpenseFlowWorker>();

var host = builder.Build();
using (var scope = host.Services.CreateScope())
{
    var sp = scope.ServiceProvider;
    // Fuerza validación de opciones antes de migrar (ValidateOnStart corre en StartAsync, más tarde).
    _ = sp.GetRequiredService<IOptions<AzureDocumentIntelligenceOptions>>().Value;
    _ = sp.GetRequiredService<IOptions<StorageOptions>>().Value;
    _ = sp.GetRequiredService<IOptions<WorkerOptions>>().Value;

    var db = sp.GetRequiredService<ExpenseFlowDbContext>();
    db.Database.Migrate();
}

host.Run();
