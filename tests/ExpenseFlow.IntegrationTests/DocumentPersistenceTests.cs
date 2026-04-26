using ExpenseFlow.Domain.Entities;
using ExpenseFlow.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ExpenseFlow.IntegrationTests;

public class DocumentPersistenceTests
{
    [Fact]
    public async Task Can_insert_and_read_Document_in_sqlite()
    {
        var dbFile = Path.Combine(Path.GetTempPath(), $"expenseflow_test_{Guid.NewGuid():N}.db");
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = dbFile,
        }.ConnectionString;
        var options = new DbContextOptionsBuilder<ExpenseFlowDbContext>()
            .UseSqlite(connectionString)
            .Options;

        try
        {
            await using (var first = new ExpenseFlowDbContext(options))
            {
                await first.Database.MigrateAsync();
            }

            const string path = "/inbox/ticket.png";
            const string hash = "a1b2c3d4e5f6";
            var createdAt = new DateTimeOffset(2026, 4, 1, 12, 0, 0, TimeSpan.Zero);
            int documentId;

            await using (var write = new ExpenseFlowDbContext(options))
            {
                var document = new Document
                {
                    FilePath = path,
                    FileHash = hash,
                    RawJson = """{"k":"v"}""",
                    OcrStatus = "Pending",
                    ErrorMessage = null,
                    CreatedAt = createdAt,
                };
                write.Documents.Add(document);
                await write.SaveChangesAsync();
                documentId = document.Id;
            }

            await using (var read = new ExpenseFlowDbContext(options))
            {
                var loaded = await read.Documents.FindAsync(documentId);
                Assert.NotNull(loaded);
                Assert.Equal(path, loaded.FilePath);
                Assert.Equal(hash, loaded.FileHash);
                Assert.Equal(createdAt, loaded.CreatedAt);
            }
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(dbFile))
            {
                File.Delete(dbFile);
            }
        }
    }
}
