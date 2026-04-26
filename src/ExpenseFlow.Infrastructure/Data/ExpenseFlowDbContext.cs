using ExpenseFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExpenseFlow.Infrastructure.Data;

public class ExpenseFlowDbContext : DbContext
{
    public ExpenseFlowDbContext(DbContextOptions<ExpenseFlowDbContext> options)
        : base(options)
    {
    }

    public DbSet<Document> Documents => Set<Document>();

    public DbSet<DocumentLine> DocumentLines => Set<DocumentLine>();

    public DbSet<ProcessingJob> ProcessingJobs => Set<ProcessingJob>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Document>(b =>
        {
            b.ToTable("Documents");
            b.HasKey(d => d.Id);
            b.Property(d => d.FilePath).IsRequired();
            b.Property(d => d.FileHash).IsRequired();
            b.HasMany(d => d.DocumentLines)
                .WithOne(l => l.Document)
                .HasForeignKey(l => l.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasMany(d => d.ProcessingJobs)
                .WithOne(j => j.Document)
                .HasForeignKey(j => j.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DocumentLine>(b =>
        {
            b.ToTable("DocumentLines");
            b.HasKey(l => l.Id);
            b.Property(l => l.Amount).HasPrecision(18, 2);
        });

        modelBuilder.Entity<ProcessingJob>(b =>
        {
            b.ToTable("ProcessingJobs");
            b.HasKey(j => j.Id);
        });
    }
}
