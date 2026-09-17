namespace FlakeGuard.Core.Domain;

public class TestRunEvent
{
    public Guid Id { get; set; }
    public Guid RepositoryId { get; set; }
    public required string CommitSha { get; set; }
    public required string Branch { get; set; }
    public required string CiRunUrl { get; set; }
    public DateTimeOffset ReceivedAt { get; set; }

    public List<TestCaseResult> Results { get; set; } = [];
}
