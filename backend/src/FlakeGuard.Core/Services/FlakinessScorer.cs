using FlakeGuard.Core.Domain;

namespace FlakeGuard.Core.Services;

public class FlakinessThresholds
{
    public double SuspiciousFlipRate { get; init; } = 0.15;
    public double QuarantineFlipRate { get; init; } = 0.30;
    public int MinRunsForSignal { get; init; } = 5;
    public int CleanRunsToRecover { get; init; } = 10;
}

public record ScoringResult(TestCaseStatus Status, double FlipRate, double FailureRate, int ConsecutiveCleanRuns);

public class FlakinessScorer(FlakinessThresholds? thresholds = null)
{
    private readonly FlakinessThresholds _thresholds = thresholds ?? new FlakinessThresholds();

    public ScoringResult Score(TestCaseStatus currentStatus, int currentConsecutiveCleanRuns, IReadOnlyList<TestOutcome> recentOutcomesOldestFirst)
    {
        if (recentOutcomesOldestFirst.Count == 0)
        {
            return new ScoringResult(currentStatus, 0, 0, currentConsecutiveCleanRuns);
        }

        var total = recentOutcomesOldestFirst.Count;
        var failures = recentOutcomesOldestFirst.Count(o => o == TestOutcome.Failed);
        var failureRate = (double)failures / total;

        var flips = 0;
        for (var i = 1; i < total; i++)
        {
            if (recentOutcomesOldestFirst[i] != recentOutcomesOldestFirst[i - 1])
            {
                flips++;
            }
        }
        var flipRate = total > 1 ? (double)flips / (total - 1) : 0;

        // A consistently failing test is broken, not flaky - only an inconsistent
        // pass/fail pattern counts as a flakiness signal.
        var isFlakySignal = total >= _thresholds.MinRunsForSignal && failureRate > 0 && failureRate < 1;
        var flakySignalStrength = isFlakySignal ? flipRate : 0.0;

        var latestOutcome = recentOutcomesOldestFirst[^1];
        var consecutiveCleanRuns = latestOutcome == TestOutcome.Passed ? currentConsecutiveCleanRuns + 1 : 0;

        TestCaseStatus status;
        if (flakySignalStrength >= _thresholds.QuarantineFlipRate)
        {
            status = TestCaseStatus.Quarantined;
        }
        else if (flakySignalStrength >= _thresholds.SuspiciousFlipRate)
        {
            status = currentStatus == TestCaseStatus.Quarantined ? TestCaseStatus.Quarantined : TestCaseStatus.Suspicious;
        }
        else if (currentStatus == TestCaseStatus.Quarantined)
        {
            // Hysteresis: only leave quarantine after a clean streak, not the moment the signal weakens.
            status = consecutiveCleanRuns >= _thresholds.CleanRunsToRecover ? TestCaseStatus.Stable : TestCaseStatus.Quarantined;
        }
        else
        {
            status = TestCaseStatus.Stable;
        }

        if (status == TestCaseStatus.Stable)
        {
            consecutiveCleanRuns = 0;
        }

        return new ScoringResult(status, flipRate, failureRate, consecutiveCleanRuns);
    }
}
