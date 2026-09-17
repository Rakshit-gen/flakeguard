namespace FlakeGuard.Core.Services;

/// <summary>The outbox payload shape for OutboxMessageType.TestRunIngested.</summary>
public record TestRunIngestedPayload(Guid RepositoryId, Guid TestRunEventId);
