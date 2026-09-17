namespace FlakeGuard.Api.Contracts;

public record IngestTestRunRequest(
    string CommitSha,
    string Branch,
    string CiRunUrl,
    List<IngestedTestResult> Results);

public record IngestedTestResult(
    string SuiteName,
    string TestName,
    string Outcome,
    long DurationMs,
    string? FailureMessage);
