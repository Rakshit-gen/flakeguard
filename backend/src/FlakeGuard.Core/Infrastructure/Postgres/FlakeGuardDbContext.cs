using FlakeGuard.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace FlakeGuard.Core.Infrastructure.Postgres;

public class FlakeGuardDbContext(DbContextOptions<FlakeGuardDbContext> options) : DbContext(options)
{
    public DbSet<Repository> Repositories => Set<Repository>();
    public DbSet<TestCase> TestCases => Set<TestCase>();
    public DbSet<TestRunEvent> TestRunEvents => Set<TestRunEvent>();
    public DbSet<TestCaseResult> TestCaseResults => Set<TestCaseResult>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Repository>(entity =>
        {
            entity.HasIndex(r => new { r.Owner, r.Name }).IsUnique();
        });

        modelBuilder.Entity<TestCase>(entity =>
        {
            entity.HasIndex(t => new { t.RepositoryId, t.SuiteName, t.TestName }).IsUnique();
            entity.HasIndex(t => new { t.RepositoryId, t.Status });
        });

        modelBuilder.Entity<TestRunEvent>(entity =>
        {
            entity.HasIndex(r => r.RepositoryId);
            entity.HasMany(r => r.Results)
                .WithOne()
                .HasForeignKey(r => r.TestRunEventId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TestCaseResult>(entity =>
        {
            entity.HasIndex(r => r.TestCaseId);
        });

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.HasIndex(o => o.ProcessedAt);
        });
    }
}
