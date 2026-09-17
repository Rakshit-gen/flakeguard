using FlakeGuard.Core.Domain;
using FlakeGuard.Core.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FlakeGuard.Core.Infrastructure.Postgres;

public class PostgresTestRunRepository(FlakeGuardDbContext db) : ITestRunRepository
{
    public async Task IngestAsync(TestRunEvent runEvent, OutboxMessage outboxMessage)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();

        db.TestRunEvents.Add(runEvent);
        db.OutboxMessages.Add(outboxMessage);
        await db.SaveChangesAsync();

        await transaction.CommitAsync();
    }

    public Task<TestRunEvent?> GetByIdAsync(Guid id) =>
        db.TestRunEvents.Include(e => e.Results).FirstOrDefaultAsync(e => e.Id == id);
}
