using FlakeGuard.Core.Domain;

namespace FlakeGuard.Core.Repositories;

public interface IOutboxRepository
{
    Task<List<OutboxMessage>> GetUnprocessedAsync(int batchSize);
    Task MarkProcessedAsync(Guid id);
    Task MarkFailedAsync(Guid id, string error);
}
