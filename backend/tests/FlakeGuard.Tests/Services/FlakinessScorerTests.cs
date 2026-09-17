using FlakeGuard.Core.Domain;
using FlakeGuard.Core.Services;
using Xunit;

namespace FlakeGuard.Tests.Services;

public class FlakinessScorerTests
{
    private static readonly TestOutcome Pass = TestOutcome.Passed;
    private static readonly TestOutcome Fail = TestOutcome.Failed;

    [Fact]
    public void AllPassingRuns_StaysStable()
    {
        var scorer = new FlakinessScorer();
        var outcomes = Enumerable.Repeat(Pass, 10).ToList();

        var result = scorer.Score(TestCaseStatus.Stable, currentConsecutiveCleanRuns: 0, outcomes);

        Assert.Equal(TestCaseStatus.Stable, result.Status);
        Assert.Equal(0, result.FlipRate);
        Assert.Equal(0, result.FailureRate);
    }

    [Fact]
    public void AllFailingRuns_IsBrokenNotFlaky_NeverQuarantined()
    {
        var scorer = new FlakinessScorer();
        var outcomes = Enumerable.Repeat(Fail, 10).ToList();

        var result = scorer.Score(TestCaseStatus.Stable, currentConsecutiveCleanRuns: 0, outcomes);

        Assert.Equal(TestCaseStatus.Stable, result.Status);
        Assert.Equal(1.0, result.FailureRate);
    }

    [Fact]
    public void HighFlipRate_QuarantinesImmediately()
    {
        var scorer = new FlakinessScorer();
        var outcomes = new List<TestOutcome> { Pass, Fail, Pass, Fail, Pass, Fail };

        var result = scorer.Score(TestCaseStatus.Stable, currentConsecutiveCleanRuns: 0, outcomes);

        Assert.Equal(TestCaseStatus.Quarantined, result.Status);
    }

    [Fact]
    public void ModerateFlipRate_MarksSuspiciousNotQuarantined()
    {
        var scorer = new FlakinessScorer();
        // 20 runs, one flip in the middle: flip rate = 1/19 ~= 0.053... too low.
        // Use a pattern that lands between the suspicious (0.15) and quarantine (0.30) thresholds.
        var outcomes = new List<TestOutcome>
        {
            Pass, Pass, Pass, Fail, Pass, Pass, Pass, Pass, Pass, Pass,
            Pass, Pass, Pass, Fail, Pass, Pass, Pass, Pass, Pass, Pass,
        };

        var result = scorer.Score(TestCaseStatus.Stable, currentConsecutiveCleanRuns: 0, outcomes);

        Assert.Equal(TestCaseStatus.Suspicious, result.Status);
        Assert.InRange(result.FlipRate, 0.15, 0.30);
    }

    [Fact]
    public void FewerRunsThanMinimum_NeverSignalsFlaky_EvenIfAlternating()
    {
        var scorer = new FlakinessScorer(new FlakinessThresholds { MinRunsForSignal = 5 });
        var outcomes = new List<TestOutcome> { Pass, Fail, Pass };

        var result = scorer.Score(TestCaseStatus.Stable, currentConsecutiveCleanRuns: 0, outcomes);

        Assert.Equal(TestCaseStatus.Stable, result.Status);
    }

    [Fact]
    public void Quarantined_StaysQuarantined_UntilConsecutiveCleanRunThresholdReached()
    {
        var thresholds = new FlakinessThresholds { CleanRunsToRecover = 10 };
        var scorer = new FlakinessScorer(thresholds);

        var status = TestCaseStatus.Quarantined;
        var consecutiveCleanRuns = 0;

        for (var i = 0; i < 9; i++)
        {
            var result = scorer.Score(status, consecutiveCleanRuns, [Pass]);
            status = result.Status;
            consecutiveCleanRuns = result.ConsecutiveCleanRuns;
        }

        Assert.Equal(TestCaseStatus.Quarantined, status);
        Assert.Equal(9, consecutiveCleanRuns);

        var finalResult = scorer.Score(status, consecutiveCleanRuns, [Pass]);

        Assert.Equal(TestCaseStatus.Stable, finalResult.Status);
    }

    [Fact]
    public void Quarantined_SingleFailureMidStreak_ResetsCleanRunCounter()
    {
        var scorer = new FlakinessScorer(new FlakinessThresholds { CleanRunsToRecover = 10 });

        var afterPasses = scorer.Score(TestCaseStatus.Quarantined, 4, [Pass]);
        Assert.Equal(TestCaseStatus.Quarantined, afterPasses.Status);
        Assert.Equal(5, afterPasses.ConsecutiveCleanRuns);

        var afterFailure = scorer.Score(afterPasses.Status, afterPasses.ConsecutiveCleanRuns, [Fail]);

        Assert.Equal(0, afterFailure.ConsecutiveCleanRuns);
    }
}
