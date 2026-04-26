using ExpenseFlow.Infrastructure;
using ExpenseFlow.Infrastructure.Data;
using ExpenseFlow.Worker;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddPersistence(builder.Configuration, builder.Environment);
builder.Services.AddFileScanning(builder.Configuration);
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ExpenseFlowDbContext>();
    db.Database.Migrate();
}

host.Run();
