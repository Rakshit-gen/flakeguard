using FlakeGuard.Core.Domain;
using FlakeGuard.Core.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FlakeGuard.Core.Infrastructure.Postgres;

public class PostgresOutboxRepository(FlakeGuardDbContext db) : IOutboxRepository
{
    public Task<List<OutboxMessage>> GetUnprocessedAsync(int batchSize) =>
        db.OutboxMessages
            .Where(o => o.ProcessedAt == null)
            .OrderBy(o => o.CreatedAt)
            .Take(batchSize)
            .ToListAsync();

    public async Task MarkProcessedAsync(Guid id)
    {
        var message = await db.OutboxMessages.FirstOrDefaultAsync(o => o.Id == id);
        if (message is null)
        {
            return;
        }
        message.ProcessedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
    }

    public async Task MarkFailedAsync(Guid id, string error)
    {
        var message = await db.OutboxMessages.FirstOrDefaultAsync(o => o.Id == id);
        if (message is null)
        {
            return;
        }
        message.Attempts++;
        message.LastError = error;
        await db.SaveChangesAsync();
    }
}
