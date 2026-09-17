using FlakeGuard.Core.Domain;

namespace FlakeGuard.Core.Repositories;

public interface ITestRunRepository
{
    /// <summary>Persists the run, its results, and an outbox message in one transaction.</summary>
    Task IngestAsync(TestRunEvent runEvent, OutboxMessage outboxMessage);

    Task<TestRunEvent?> GetByIdAsync(Guid id);
}
