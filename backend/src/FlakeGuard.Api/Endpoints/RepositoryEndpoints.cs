using System.Security.Cryptography;
using FlakeGuard.Api.Contracts;
using FlakeGuard.Core.Domain;
using FlakeGuard.Core.Repositories;

namespace FlakeGuard.Api.Endpoints;

public static class RepositoryEndpoints
{
    public static void MapRepositoryEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/repositories").WithTags("Repositories");

        group.MapPost("/", RegisterRepository);
        group.MapGet("/", ListRepositories);
        group.MapGet("/{id:guid}/tests", ListTests);
        group.MapGet("/{id:guid}/quarantine", ListQuarantined);
    }

    private static async Task<IResult> RegisterRepository(
        RegisterRepositoryRequest request, IRepositoryRepository repositoryRepository)
    {
        var existing = await repositoryRepository.GetByOwnerAndNameAsync(request.Owner, request.Name);
        if (existing is not null)
        {
            return Results.Conflict(new { error = $"{request.Owner}/{request.Name} is already registered." });
        }

        var repository = new Repository
        {
            Id = Guid.NewGuid(),
            Owner = request.Owner,
            Name = request.Name,
            WebhookSecret = GenerateWebhookSecret(),
            GitHubInstallationToken = request.GitHubInstallationToken,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        await repositoryRepository.AddAsync(repository);

        return Results.Created(
            $"/api/repositories/{repository.Id}",
            new RegisterRepositoryResponse(repository.Id, repository.Owner, repository.Name, repository.WebhookSecret));
    }

    private static async Task<IResult> ListRepositories(
        IRepositoryRepository repositoryRepository, ITestCaseRepository testCaseRepository)
    {
        var repositories = await repositoryRepository.GetAllAsync();
        var summaries = new List<RepositorySummaryResponse>();

        foreach (var repository in repositories)
        {
            var tests = await testCaseRepository.GetByRepositoryAsync(repository.Id);
            var quarantinedCount = tests.Count(t => t.Status == TestCaseStatus.Quarantined);
            summaries.Add(new RepositorySummaryResponse(
                repository.Id, repository.Owner, repository.Name, tests.Count, quarantinedCount));
        }

        return Results.Ok(summaries);
    }

    private static async Task<IResult> ListTests(
        Guid id, IRepositoryRepository repositoryRepository, ITestCaseRepository testCaseRepository)
    {
        var repository = await repositoryRepository.GetByIdAsync(id);
        if (repository is null)
        {
            return Results.NotFound();
        }

        var tests = await testCaseRepository.GetByRepositoryAsync(id);
        var responses = await Task.WhenAll(tests.Select(t => ToResponseAsync(t, testCaseRepository)));
        return Results.Ok(responses);
    }

    private static async Task<IResult> ListQuarantined(
        Guid id, IRepositoryRepository repositoryRepository, ITestCaseRepository testCaseRepository)
    {
        var repository = await repositoryRepository.GetByIdAsync(id);
        if (repository is null)
        {
            return Results.NotFound();
        }

        var tests = await testCaseRepository.GetQuarantinedAsync(id);
        var responses = await Task.WhenAll(tests.Select(t => ToResponseAsync(t, testCaseRepository)));
        return Results.Ok(responses);
    }

    private static async Task<TestCaseResponse> ToResponseAsync(TestCase testCase, ITestCaseRepository testCaseRepository)
    {
        var recentOutcomes = await testCaseRepository.GetRecentOutcomesAsync(testCase.Id, windowSize: 30);
        return new TestCaseResponse(
            testCase.Id,
            testCase.SuiteName,
            testCase.TestName,
            testCase.Status.ToString(),
            testCase.FlipRate,
            testCase.FailureRate,
            testCase.LastUpdatedAt,
            testCase.QuarantinedAt,
            recentOutcomes.Select(o => o.ToString()).ToList());
    }

    private static string GenerateWebhookSecret() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
}
