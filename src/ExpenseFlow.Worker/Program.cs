using ExpenseFlow.Application.Options;
using ExpenseFlow.Infrastructure;
using ExpenseFlow.Infrastructure.Data;
using ExpenseFlow.Worker;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.Configure<WorkerOptions>(
    builder.Configuration.GetSection(WorkerOptions.SectionName));
builder.Services.AddPersistence(builder.Configuration, builder.Environment);
builder.Services.AddFileScanning(builder.Configuration);
builder.Services.AddFileStorage();
builder.Services.AddOcrProviders(builder.Configuration);
builder.Services.AddReceiptNormalization();
builder.Services.AddHostedService<ExpenseFlowWorker>();

var host = builder.Build();
using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ExpenseFlowDbContext>();
    db.Database.Migrate();
}

host.Run();
