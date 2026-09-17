using System.Text;
using System.Text.Json;
using FlakeGuard.Api.Contracts;
using FlakeGuard.Core.Domain;
using FlakeGuard.Core.Repositories;
using FlakeGuard.Core.Services;

namespace FlakeGuard.Api.Endpoints;

public static class IngestionEndpoints
{
    private const string SignatureHeader = "X-FlakeGuard-Signature";

    public static void MapIngestionEndpoints(this WebApplication app)
    {
        app.MapPost("/api/ingest/{owner}/{name}", Ingest).WithTags("Ingestion");
    }

    private static async Task<IResult> Ingest(
        string owner,
        string name,
        HttpRequest request,
        IRepositoryRepository repositoryRepository,
        TestCaseRegistry testCaseRegistry,
        ITestRunRepository testRunRepository,
        OutboxSignal outboxSignal)
    {
        var repository = await repositoryRepository.GetByOwnerAndNameAsync(owner, name);
        if (repository is null)
        {
            return Results.NotFound(new { error = $"Repository {owner}/{name} is not registered." });
        }

        request.EnableBuffering();
        using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
        var rawBody = await reader.ReadToEndAsync();
        request.Body.Position = 0;

        var signature = request.Headers[SignatureHeader].FirstOrDefault();
        if (!WebhookSignature.Verify(repository.WebhookSecret, Encoding.UTF8.GetBytes(rawBody), signature))
        {
            return Results.Unauthorized();
        }

        IngestTestRunRequest? payload;
        try
        {
            payload = JsonSerializer.Deserialize<IngestTestRunRequest>(
                rawBody, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        }
        catch (JsonException)
        {
            return Results.BadRequest(new { error = "Malformed JSON payload." });
        }

        if (payload is null || payload.Results.Count == 0)
        {
            return Results.BadRequest(new { error = "At least one test result is required." });
        }

        var runEvent = new TestRunEvent
        {
            Id = Guid.NewGuid(),
            RepositoryId = repository.Id,
            CommitSha = payload.CommitSha,
            Branch = payload.Branch,
            CiRunUrl = payload.CiRunUrl,
            ReceivedAt = DateTimeOffset.UtcNow,
        };

        foreach (var incoming in payload.Results)
        {
            if (!Enum.TryParse<TestOutcome>(incoming.Outcome, ignoreCase: true, out var outcome))
            {
                return Results.BadRequest(new { error = $"Unknown outcome '{incoming.Outcome}'." });
            }

            var testCase = await testCaseRegistry.GetOrCreateAsync(repository.Id, incoming.SuiteName, incoming.TestName);
            runEvent.Results.Add(new TestCaseResult
            {
                Id = Guid.NewGuid(),
                TestRunEventId = runEvent.Id,
                TestCaseId = testCase.Id,
                SuiteName = incoming.SuiteName,
                TestName = incoming.TestName,
                Outcome = outcome,
                DurationMs = incoming.DurationMs,
                FailureMessage = incoming.FailureMessage,
            });
        }

        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = OutboxMessageType.TestRunIngested,
            PayloadJson = JsonSerializer.Serialize(new TestRunIngestedPayload(repository.Id, runEvent.Id)),
            CreatedAt = DateTimeOffset.UtcNow,
        };

        await testRunRepository.IngestAsync(runEvent, outboxMessage);
        outboxSignal.NotifyWritten();

        return Results.Accepted(value: new { runEvent.Id, resultCount = runEvent.Results.Count });
    }
}
