using FlakeGuard.Core.Domain;

namespace FlakeGuard.Core.Services;

public interface IQuarantineNotifier
{
    Task NotifyAsync(Repository repository, string commitSha, IReadOnlyList<TestCase> newlyQuarantined);
}
