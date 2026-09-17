using FlakeGuard.Core.Domain;
using FlakeGuard.Core.Repositories;

namespace FlakeGuard.Core.Services;

/// <summary>
/// Consumes an already-persisted TestRunEvent (its results already carry a resolved TestCaseId,
/// assigned by TestCaseRegistry at ingestion time) and re-scores each affected test.
/// </summary>
public class TestRunProcessor(
    ITestCaseRepository testCaseRepository,
    IQuarantineNotifier quarantineNotifier,
    FlakinessScorer scorer)
{
    private const int ScoringWindowSize = 20;

    public async Task ProcessAsync(Repository repository, TestRunEvent runEvent)
    {
        var newlyQuarantined = new List<TestCase>();

        foreach (var result in runEvent.Results)
        {
            var testCase = await testCaseRepository.GetByIdAsync(result.TestCaseId)
                ?? throw new InvalidOperationException(
                    $"TestCase {result.TestCaseId} was not found; it must be registered before ingestion completes.");

            var previousStatus = testCase.Status;

            var recentOutcomes = await testCaseRepository.GetRecentOutcomesAsync(testCase.Id, ScoringWindowSize);
            var scoreResult = scorer.Score(testCase.Status, testCase.ConsecutiveCleanRuns, recentOutcomes);

            testCase.Status = scoreResult.Status;
            testCase.FlipRate = scoreResult.FlipRate;
            testCase.FailureRate = scoreResult.FailureRate;
            testCase.ConsecutiveCleanRuns = scoreResult.ConsecutiveCleanRuns;
            testCase.LastUpdatedAt = runEvent.ReceivedAt;

            if (previousStatus != TestCaseStatus.Quarantined && scoreResult.Status == TestCaseStatus.Quarantined)
            {
                testCase.QuarantinedAt = runEvent.ReceivedAt;
                newlyQuarantined.Add(testCase);
            }

            await testCaseRepository.UpdateAsync(testCase);
        }

        if (newlyQuarantined.Count > 0)
        {
            await quarantineNotifier.NotifyAsync(repository, runEvent.CommitSha, newlyQuarantined);
        }
    }
}
