namespace FlakeGuard.Core.Domain;

public enum TestCaseStatus
{
    Stable,
    Suspicious,
    Quarantined,
}

public class TestCase
{
    public Guid Id { get; set; }
    public Guid RepositoryId { get; set; }
    public required string SuiteName { get; set; }
    public required string TestName { get; set; }

    public TestCaseStatus Status { get; set; } = TestCaseStatus.Stable;
    public double FlipRate { get; set; }
    public double FailureRate { get; set; }
    public int ConsecutiveCleanRuns { get; set; }
    public DateTimeOffset? QuarantinedAt { get; set; }
    public DateTimeOffset LastUpdatedAt { get; set; }

    public string FullyQualifiedName => $"{SuiteName}::{TestName}";
}
