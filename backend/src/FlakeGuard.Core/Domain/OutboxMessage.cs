namespace FlakeGuard.Core.Domain;

public enum OutboxMessageType
{
    TestRunIngested,
}

public class OutboxMessage
{
    public Guid Id { get; set; }
    public OutboxMessageType Type { get; set; }
    public required string PayloadJson { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public int Attempts { get; set; }
    public string? LastError { get; set; }
}
