using FlakeGuard.Core.Domain;
using FlakeGuard.Core.Repositories;

namespace FlakeGuard.Core.Services;

/// <summary>Resolves the durable TestCase identity for an incoming result, creating it on first sight.</summary>
public class TestCaseRegistry(ITestCaseRepository testCaseRepository)
{
    public async Task<TestCase> GetOrCreateAsync(Guid repositoryId, string suiteName, string testName)
    {
        var existing = await testCaseRepository.GetAsync(repositoryId, suiteName, testName);
        if (existing is not null)
        {
            return existing;
        }

        var testCase = new TestCase
        {
            Id = Guid.NewGuid(),
            RepositoryId = repositoryId,
            SuiteName = suiteName,
            TestName = testName,
            LastUpdatedAt = DateTimeOffset.UtcNow,
        };
        await testCaseRepository.AddAsync(testCase);
        return testCase;
    }
}
