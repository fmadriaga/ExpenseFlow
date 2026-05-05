using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ExpenseFlow.Infrastructure.Data;

/// <summary>
/// Factory used exclusively by EF Core design-time tools (dotnet-ef migrations, database update, etc.).
/// Provides a DbContext without starting the full Worker host, avoiding startup hangs during tooling.
/// </summary>
public sealed class ExpenseFlowDbContextFactory : IDesignTimeDbContextFactory<ExpenseFlowDbContext>
{
    public ExpenseFlowDbContext CreateDbContext(string[] args)
    {
        // Resolve the DB path relative to this file's location at design time:
        // Infrastructure project is at src/ExpenseFlow.Infrastructure/
        // DB lives at data/expenseflow.db (two levels up from the project root)
        var infraDir = Path.GetDirectoryName(typeof(ExpenseFlowDbContextFactory).Assembly.Location)
                       ?? Directory.GetCurrentDirectory();

        // Walk up to the solution root (src/ExpenseFlow.Infrastructure/bin/Debug/net9.0 -> root)
        var solutionRoot = infraDir;
        for (var i = 0; i < 5; i++)
        {
            if (File.Exists(Path.Combine(solutionRoot, "ExpenseFlow.sln")))
                break;
            solutionRoot = Path.GetDirectoryName(solutionRoot) ?? solutionRoot;
        }

        var dbPath = Path.GetFullPath(Path.Combine(solutionRoot, "data", "expenseflow.db"));

        var optionsBuilder = new DbContextOptionsBuilder<ExpenseFlowDbContext>();
        optionsBuilder.UseSqlite($"Data Source={dbPath}");

        return new ExpenseFlowDbContext(optionsBuilder.Options);
    }
}
