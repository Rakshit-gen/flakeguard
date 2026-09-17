using FlakeGuard.Core.Domain;
using FlakeGuard.Core.Services;
using FlakeGuard.Tests.Fakes;
using Xunit;

namespace FlakeGuard.Tests.Services;

public class TestRunProcessorTests
{
    private static Repository MakeRepository() => new()
    {
        Id = Guid.NewGuid(),
        Owner = "acme",
        Name = "widgets",
        WebhookSecret = "secret",
        CreatedAt = DateTimeOffset.UtcNow,
    };

    private static TestRunEvent MakeRunEvent(Guid repositoryId, params TestCaseResult[] results) => new()
    {
        Id = Guid.NewGuid(),
        RepositoryId = repositoryId,
        CommitSha = "abc123",
        Branch = "main",
        CiRunUrl = "https://ci.example.com/run/1",
        ReceivedAt = DateTimeOffset.UtcNow,
        Results = results.ToList(),
    };

    private static async Task<Guid> RegisterTestCase(
        InMemoryTestCaseRepository repository, Guid repositoryId, string suite, string test)
    {
        var testCaseId = Guid.NewGuid();
        await repository.AddAsync(new TestCase
        {
            Id = testCaseId,
            RepositoryId = repositoryId,
            SuiteName = suite,
            TestName = test,
            Status = TestCaseStatus.Stable,
        });
        return testCaseId;
    }

    [Fact]
    public async Task FirstResultForARegisteredTest_DefaultsStable()
    {
        var testCaseRepository = new InMemoryTestCaseRepository();
        var notifier = new FakeQuarantineNotifier();
        var processor = new TestRunProcessor(testCaseRepository, notifier, new FlakinessScorer());
        var repository = MakeRepository();

        var testCaseId = await RegisterTestCase(testCaseRepository, repository.Id, "Suite", "NewTest");
        testCaseRepository.RecordOutcome(testCaseId, TestOutcome.Passed);

        var result = new TestCaseResult
        {
            TestCaseId = testCaseId, SuiteName = "Suite", TestName = "NewTest", Outcome = TestOutcome.Passed,
        };
        var runEvent = MakeRunEvent(repository.Id, result);

        await processor.ProcessAsync(repository, runEvent);

        var testCase = await testCaseRepository.GetByIdAsync(testCaseId);
        Assert.Equal(TestCaseStatus.Stable, testCase!.Status);
    }

    [Fact]
    public async Task ResultForUnregisteredTestCase_Throws()
    {
        var testCaseRepository = new InMemoryTestCaseRepository();
        var notifier = new FakeQuarantineNotifier();
        var processor = new TestRunProcessor(testCaseRepository, notifier, new FlakinessScorer());
        var repository = MakeRepository();

        var result = new TestCaseResult
        {
            TestCaseId = Guid.NewGuid(), SuiteName = "Suite", TestName = "Ghost", Outcome = TestOutcome.Passed,
        };
        var runEvent = MakeRunEvent(repository.Id, result);

        await Assert.ThrowsAsync<InvalidOperationException>(() => processor.ProcessAsync(repository, runEvent));
    }

    [Fact]
    public async Task TestThatFlipsOnEveryRun_GetsQuarantinedAndNotifiedOnce()
    {
        var testCaseRepository = new InMemoryTestCaseRepository();
        var notifier = new FakeQuarantineNotifier();
        var processor = new TestRunProcessor(testCaseRepository, notifier, new FlakinessScorer());
        var repository = MakeRepository();

        var testCaseId = await RegisterTestCase(testCaseRepository, repository.Id, "Suite", "FlakyTest");
        testCaseRepository.SeedOutcomes(testCaseId, [
            TestOutcome.Passed, TestOutcome.Failed, TestOutcome.Passed, TestOutcome.Failed, TestOutcome.Passed,
        ]);

        var result = new TestCaseResult
        {
            TestCaseId = testCaseId, SuiteName = "Suite", TestName = "FlakyTest", Outcome = TestOutcome.Failed,
        };
        var runEvent = MakeRunEvent(repository.Id, result);
        testCaseRepository.RecordOutcome(testCaseId, TestOutcome.Failed);

        await processor.ProcessAsync(repository, runEvent);

        var testCase = await testCaseRepository.GetByIdAsync(testCaseId);
        Assert.Equal(TestCaseStatus.Quarantined, testCase!.Status);
        Assert.NotNull(testCase.QuarantinedAt);
        Assert.Single(notifier.Notifications);
        Assert.Single(notifier.Notifications[0].Tests);

        // A second run that is still quarantined must not trigger a second notification.
        var secondRunEvent = MakeRunEvent(repository.Id, result);
        testCaseRepository.RecordOutcome(testCaseId, TestOutcome.Failed);
        await processor.ProcessAsync(repository, secondRunEvent);

        Assert.Single(notifier.Notifications);
    }
}
