using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FlakeGuard.Api.Contracts;
using FlakeGuard.Core.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace FlakeGuard.Tests.Integration;

/// <summary>
/// Exercises the real Postgres-backed stack end to end. Needs ConnectionStrings__Postgres
/// pointing at a live database, which only the CI job provides via a service container.
/// </summary>
public class IngestionEndpointTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task AlternatingRuns_TripQuarantine_AndShowUpInTheQuarantineList()
    {
        var owner = "acme";
        var name = $"widgets-{Guid.NewGuid():N}";

        var registerResponse = await _client.PostAsJsonAsync(
            "/api/repositories", new RegisterRepositoryRequest(owner, name, GitHubInstallationToken: null));
        registerResponse.EnsureSuccessStatusCode();
        var registered = await registerResponse.Content.ReadFromJsonAsync<RegisterRepositoryResponse>();
        Assert.NotNull(registered);

        var outcomes = new[] { "Passed", "Failed", "Passed", "Failed", "Passed", "Failed" };
        foreach (var outcome in outcomes)
        {
            await SendSignedRun(owner, name, registered!.WebhookSecret, outcome);
        }

        var quarantined = await PollUntilQuarantined(registered!.Id);

        Assert.NotNull(quarantined);
        var test = Assert.Single(quarantined!);
        Assert.Equal("FlakyTest", test.TestName);
    }

    private async Task<List<TestCaseResponse>?> PollUntilQuarantined(Guid repositoryId)
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var response = await _client.GetAsync($"/api/repositories/{repositoryId}/quarantine");
            response.EnsureSuccessStatusCode();
            var quarantined = await response.Content.ReadFromJsonAsync<List<TestCaseResponse>>();
            if (quarantined is { Count: > 0 })
            {
                return quarantined;
            }
            await Task.Delay(250);
        }
        return null;
    }

    private async Task SendSignedRun(string owner, string name, string secret, string outcome)
    {
        var payload = new IngestTestRunRequest(
            CommitSha: Guid.NewGuid().ToString("N")[..7],
            Branch: "main",
            CiRunUrl: "https://ci.example.com/run/1",
            Results: [new IngestedTestResult("Suite", "FlakyTest", outcome, DurationMs: 120, FailureMessage: null)]);

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var signature = WebhookSignature.Compute(secret, Encoding.UTF8.GetBytes(json));

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/ingest/{owner}/{name}")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
        request.Headers.Add("X-FlakeGuard-Signature", signature);

        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }
}
