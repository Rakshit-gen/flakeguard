namespace FlakeGuard.Core.Domain;

public enum TestOutcome
{
    Passed,
    Failed,
}

public class TestCaseResult
{
    public Guid Id { get; set; }
    public Guid TestRunEventId { get; set; }
    public Guid TestCaseId { get; set; }
    public required string SuiteName { get; set; }
    public required string TestName { get; set; }
    public TestOutcome Outcome { get; set; }
    public long DurationMs { get; set; }
    public string? FailureMessage { get; set; }
}
