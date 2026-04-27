using ExpenseFlow.Domain.Entities;
using ExpenseFlow.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ExpenseFlow.IntegrationTests;

/// <summary>
/// Verifica índice único (FamilyId + FileHash) y que el segundo insert falle con
/// <see cref="DbUpdateException"/> (comportamiento esperado cuando el Worker capta duplicados).
/// </summary>
public sealed class FileHashUniqueIndexTests
{
    [Fact]
    public async Task Second_document_with_same_FileHash_throws_DbUpdateException()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"ef_filehash_unique_{Guid.NewGuid():N}.db");
        var connectionString = new SqliteConnectionStringBuilder { DataSource = dbPath }.ConnectionString;
        var options = new DbContextOptionsBuilder<ExpenseFlowDbContext>()
            .UseSqlite(connectionString)
            .Options;

        try
        {
            await using (var db = new ExpenseFlowDbContext(options))
            {
                await db.Database.MigrateAsync();
                var createdAt = DateTimeOffset.UtcNow;
                var h = "ab" + new string('0', 62);
                const int familyId = 1;
                db.Documents.Add(
                    new Document
                    {
                        FamilyId = familyId,
                        FilePath = "/a.png",
                        FileHash = h,
                        OcrStatus = "Failed",
                        CreatedAt = createdAt,
                    });
                await db.SaveChangesAsync();

                db.Documents.Add(
                    new Document
                    {
                        FamilyId = familyId,
                        FilePath = "/b.png",
                        FileHash = h,
                        OcrStatus = "Failed",
                        CreatedAt = createdAt,
                    });
                var ex = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
                Assert.NotNull(ex!.InnerException);
            }
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(dbPath))
            {
                try
                {
                    File.Delete(dbPath);
                }
                catch
                {
                    // best effort
                }
            }
        }
    }
}
