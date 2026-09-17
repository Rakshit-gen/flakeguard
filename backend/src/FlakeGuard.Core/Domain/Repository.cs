namespace FlakeGuard.Core.Domain;

public class Repository
{
    public Guid Id { get; set; }
    public required string Owner { get; set; }
    public required string Name { get; set; }
    public required string WebhookSecret { get; set; }
    public string? GitHubInstallationToken { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public string FullName => $"{Owner}/{Name}";
}
