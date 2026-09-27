using FlakeGuard.Core.Domain;
using FlakeGuard.Core.Repositories;
using Microsoft.EntityFrameworkCore;

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

        try
        {
            await testCaseRepository.AddAsync(testCase);
            return testCase;
        }
        catch (DbUpdateException)
        {
            // Two concurrent first-sight ingestions for the same test (e.g. parallel CI
            // shards) can both pass the GetAsync check above; the unique index on
            // (RepositoryId, SuiteName, TestName) then rejects the loser's insert. Fall back
            // to whichever row actually won instead of failing the whole ingest request.
            var winner = await testCaseRepository.GetAsync(repositoryId, suiteName, testName);
            if (winner is not null)
            {
                return winner;
            }
            throw;
        }
    }
}
