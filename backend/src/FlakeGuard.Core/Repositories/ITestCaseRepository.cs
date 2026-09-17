using FlakeGuard.Core.Domain;

namespace FlakeGuard.Core.Repositories;

public interface ITestCaseRepository
{
    Task<TestCase?> GetAsync(Guid repositoryId, string suiteName, string testName);
    Task<TestCase?> GetByIdAsync(Guid id);
    Task<List<TestCase>> GetByRepositoryAsync(Guid repositoryId);
    Task<List<TestCase>> GetQuarantinedAsync(Guid repositoryId);
    Task AddAsync(TestCase testCase);
    Task UpdateAsync(TestCase testCase);

    /// <summary>Most recent outcomes for a test, oldest first, capped at windowSize.</summary>
    Task<List<TestOutcome>> GetRecentOutcomesAsync(Guid testCaseId, int windowSize);
}
