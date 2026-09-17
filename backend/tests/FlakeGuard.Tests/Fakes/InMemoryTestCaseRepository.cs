using FlakeGuard.Core.Domain;
using FlakeGuard.Core.Repositories;

namespace FlakeGuard.Tests.Fakes;

public class InMemoryTestCaseRepository : ITestCaseRepository
{
    private readonly List<TestCase> _testCases = [];
    private readonly Dictionary<Guid, List<TestOutcome>> _outcomesByTestCaseId = [];

    public Task<TestCase?> GetAsync(Guid repositoryId, string suiteName, string testName) =>
        Task.FromResult(_testCases.FirstOrDefault(t =>
            t.RepositoryId == repositoryId && t.SuiteName == suiteName && t.TestName == testName));

    public Task<TestCase?> GetByIdAsync(Guid id) =>
        Task.FromResult(_testCases.FirstOrDefault(t => t.Id == id));

    public Task<List<TestCase>> GetByRepositoryAsync(Guid repositoryId) =>
        Task.FromResult(_testCases.Where(t => t.RepositoryId == repositoryId).ToList());

    public Task<List<TestCase>> GetQuarantinedAsync(Guid repositoryId) =>
        Task.FromResult(_testCases
            .Where(t => t.RepositoryId == repositoryId && t.Status == TestCaseStatus.Quarantined)
            .ToList());

    public Task AddAsync(TestCase testCase)
    {
        _testCases.Add(testCase);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(TestCase testCase) => Task.CompletedTask;

    public Task<List<TestOutcome>> GetRecentOutcomesAsync(Guid testCaseId, int windowSize)
    {
        var outcomes = _outcomesByTestCaseId.GetValueOrDefault(testCaseId, []);
        return Task.FromResult(outcomes.TakeLast(windowSize).ToList());
    }

    public void SeedOutcomes(Guid testCaseId, IEnumerable<TestOutcome> outcomes)
    {
        _outcomesByTestCaseId[testCaseId] = outcomes.ToList();
    }

    public void RecordOutcome(Guid testCaseId, TestOutcome outcome)
    {
        if (!_outcomesByTestCaseId.TryGetValue(testCaseId, out var list))
        {
            list = [];
            _outcomesByTestCaseId[testCaseId] = list;
        }
        list.Add(outcome);
    }
}
