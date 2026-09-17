using FlakeGuard.Core.Domain;
using FlakeGuard.Core.Services;

namespace FlakeGuard.Tests.Fakes;

public class FakeQuarantineNotifier : IQuarantineNotifier
{
    public List<(Repository Repository, string CommitSha, List<TestCase> Tests)> Notifications { get; } = [];

    public Task NotifyAsync(Repository repository, string commitSha, IReadOnlyList<TestCase> newlyQuarantined)
    {
        Notifications.Add((repository, commitSha, newlyQuarantined.ToList()));
        return Task.CompletedTask;
    }
}
