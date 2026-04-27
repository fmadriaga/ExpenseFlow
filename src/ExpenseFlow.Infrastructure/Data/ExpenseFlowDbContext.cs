using ExpenseFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExpenseFlow.Infrastructure.Data;

public class ExpenseFlowDbContext : DbContext
{
    public ExpenseFlowDbContext(DbContextOptions<ExpenseFlowDbContext> options)
        : base(options)
    {
    }

    public DbSet<Family> Families => Set<Family>();

    public DbSet<FamilyMember> FamilyMembers => Set<FamilyMember>();

    public DbSet<Document> Documents => Set<Document>();

    public DbSet<ExpenseSplit> ExpenseSplits => Set<ExpenseSplit>();

    public DbSet<DocumentLine> DocumentLines => Set<DocumentLine>();

    public DbSet<ProcessingJob> ProcessingJobs => Set<ProcessingJob>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Family>(b =>
        {
            b.ToTable("Families");
            b.HasKey(f => f.Id);
            b.Property(f => f.Name).IsRequired();
            b.Property(f => f.InboxPath).IsRequired();
            b.Property(f => f.ProcessedPath).IsRequired();
            b.Property(f => f.ErrorPath).IsRequired();
        });

        modelBuilder.Entity<FamilyMember>(b =>
        {
            b.ToTable("FamilyMembers");
            b.HasKey(m => m.Id);
            b.Property(m => m.Name).IsRequired();
            b.HasOne(m => m.Family)
                .WithMany(f => f.Members)
                .HasForeignKey(m => m.FamilyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Document>(b =>
        {
            b.ToTable("Documents");
            b.HasKey(d => d.Id);
            b.Property(d => d.FilePath).IsRequired();
            b.Property(d => d.FileHash).IsRequired();
            b.Property(d => d.Category).IsRequired().HasDefaultValue("otros");
            b.Property(d => d.TotalAmount).HasPrecision(18, 2);
            b.Property(d => d.TaxAmount).HasPrecision(18, 2);
            b.Property(d => d.Confidence).HasPrecision(5, 2);
            b.HasIndex(d => new { d.FamilyId, d.FileHash }).IsUnique();
            b.HasOne(d => d.Family)
                .WithMany(f => f.Documents)
                .HasForeignKey(d => d.FamilyId)
                .OnDelete(DeleteBehavior.Restrict);
            b.HasOne(d => d.PaidByMember)
                .WithMany()
                .HasForeignKey(d => d.PaidByFamilyMemberId)
                .OnDelete(DeleteBehavior.SetNull);
            b.HasMany(d => d.DocumentLines)
                .WithOne(l => l.Document)
                .HasForeignKey(l => l.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasMany(d => d.ProcessingJobs)
                .WithOne(j => j.Document)
                .HasForeignKey(j => j.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasMany(d => d.ExpenseSplits)
                .WithOne(s => s.Document)
                .HasForeignKey(s => s.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ExpenseSplit>(b =>
        {
            b.ToTable("ExpenseSplits");
            b.HasKey(s => s.Id);
            b.Property(s => s.Percentage).HasPrecision(5, 2);
            b.HasIndex(s => new { s.DocumentId, s.FamilyMemberId }).IsUnique();
            b.HasOne(s => s.FamilyMember)
                .WithMany(m => m.ExpenseSplits)
                .HasForeignKey(s => s.FamilyMemberId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DocumentLine>(b =>
        {
            b.ToTable("DocumentLines");
            b.HasKey(l => l.Id);
            b.Property(l => l.Quantity).HasPrecision(18, 4);
            b.Property(l => l.UnitPrice).HasPrecision(18, 2);
            b.Property(l => l.Amount).HasPrecision(18, 2);
        });

        modelBuilder.Entity<ProcessingJob>(b =>
        {
            b.ToTable("ProcessingJobs");
            b.HasKey(j => j.Id);
        });
    }
}
