using FlakeGuard.Core.Domain;
using FlakeGuard.Core.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FlakeGuard.Core.Infrastructure.Postgres;

public class PostgresTestCaseRepository(FlakeGuardDbContext db) : ITestCaseRepository
{
    public Task<TestCase?> GetAsync(Guid repositoryId, string suiteName, string testName) =>
        db.TestCases.FirstOrDefaultAsync(t =>
            t.RepositoryId == repositoryId && t.SuiteName == suiteName && t.TestName == testName);

    public Task<TestCase?> GetByIdAsync(Guid id) =>
        db.TestCases.FirstOrDefaultAsync(t => t.Id == id);

    public Task<List<TestCase>> GetByRepositoryAsync(Guid repositoryId) =>
        db.TestCases
            .Where(t => t.RepositoryId == repositoryId)
            .OrderByDescending(t => t.FlipRate)
            .ToListAsync();

    public Task<List<TestCase>> GetQuarantinedAsync(Guid repositoryId) =>
        db.TestCases
            .Where(t => t.RepositoryId == repositoryId && t.Status == TestCaseStatus.Quarantined)
            .OrderByDescending(t => t.QuarantinedAt)
            .ToListAsync();

    public async Task AddAsync(TestCase testCase)
    {
        db.TestCases.Add(testCase);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(TestCase testCase)
    {
        db.TestCases.Update(testCase);
        await db.SaveChangesAsync();
    }

    public async Task<List<TestOutcome>> GetRecentOutcomesAsync(Guid testCaseId, int windowSize)
    {
        var recentNewestFirst = await db.TestCaseResults
            .Where(r => r.TestCaseId == testCaseId)
            .Join(db.TestRunEvents, r => r.TestRunEventId, e => e.Id, (r, e) => new { r.Outcome, e.ReceivedAt })
            .OrderByDescending(x => x.ReceivedAt)
            .Take(windowSize)
            .Select(x => x.Outcome)
            .ToListAsync();

        recentNewestFirst.Reverse();
        return recentNewestFirst;
    }
}
