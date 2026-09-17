namespace FlakeGuard.Api.Contracts;

public record RegisterRepositoryRequest(string Owner, string Name, string? GitHubInstallationToken);

public record RegisterRepositoryResponse(Guid Id, string Owner, string Name, string WebhookSecret);

public record RepositorySummaryResponse(Guid Id, string Owner, string Name, int TotalTests, int QuarantinedCount);

public record TestCaseResponse(
    Guid Id,
    string SuiteName,
    string TestName,
    string Status,
    double FlipRate,
    double FailureRate,
    DateTimeOffset LastUpdatedAt,
    DateTimeOffset? QuarantinedAt,
    List<string> RecentOutcomes);
