using System.Net.Http.Headers;
using System.Net.Http.Json;
using FlakeGuard.Core.Domain;
using FlakeGuard.Core.Services;

namespace FlakeGuard.Api.Infrastructure;

/// <summary>Reports newly-quarantined tests via the GitHub Commit Status API rather than a PR
/// comment: one POST per commit, no comment-thread bookkeeping, and it shows up right in the
/// checks list next to the CI run itself.</summary>
public class GitHubQuarantineNotifier(HttpClient httpClient, ILogger<GitHubQuarantineNotifier> logger)
    : IQuarantineNotifier
{
    public async Task NotifyAsync(Repository repository, string commitSha, IReadOnlyList<TestCase> newlyQuarantined)
    {
        if (newlyQuarantined.Count == 0)
        {
            return;
        }

        if (string.IsNullOrEmpty(repository.GitHubInstallationToken))
        {
            logger.LogWarning(
                "Skipping GitHub status update for {Repository}: no installation token on file", repository.FullName);
            return;
        }

        var testNames = string.Join(", ", newlyQuarantined.Select(t => t.FullyQualifiedName));
        var description = testNames.Length > 140 ? testNames[..137] + "..." : testNames;

        var request = new HttpRequestMessage(
            HttpMethod.Post, $"https://api.github.com/repos/{repository.FullName}/statuses/{commitSha}")
        {
            Content = JsonContent.Create(new
            {
                state = "failure",
                context = "flakeguard/quarantine",
                description = $"Quarantined: {description}",
                target_url = $"https://github.com/{repository.FullName}/commit/{commitSha}",
            }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", repository.GitHubInstallationToken);
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue("FlakeGuard", "1.0"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

        var response = await httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            logger.LogError(
                "GitHub status update for {Repository}@{Sha} failed with {Status}: {Body}",
                repository.FullName, commitSha, response.StatusCode, body);
        }
    }
}
