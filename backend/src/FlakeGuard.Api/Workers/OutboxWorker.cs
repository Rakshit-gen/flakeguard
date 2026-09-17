using System.Text.Json;
using FlakeGuard.Core.Repositories;
using FlakeGuard.Core.Services;

namespace FlakeGuard.Api.Workers;

public class OutboxWorker(IServiceScopeFactory scopeFactory, OutboxSignal signal, ILogger<OutboxWorker> logger)
    : BackgroundService
{
    private static readonly TimeSpan PollFallbackInterval = TimeSpan.FromSeconds(15);
    private const int BatchSize = 20;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            bool processedAny;
            try
            {
                processedAny = await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Outbox batch failed unexpectedly");
                processedAny = false;
            }

            if (!processedAny)
            {
                await signal.WaitAsync(PollFallbackInterval, stoppingToken);
            }
        }
    }

    private async Task<bool> ProcessBatchAsync(CancellationToken stoppingToken)
    {
        using var scope = scopeFactory.CreateScope();
        var outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        var testRunRepository = scope.ServiceProvider.GetRequiredService<ITestRunRepository>();
        var repositoryRepository = scope.ServiceProvider.GetRequiredService<IRepositoryRepository>();
        var processor = scope.ServiceProvider.GetRequiredService<TestRunProcessor>();

        var messages = await outboxRepository.GetUnprocessedAsync(BatchSize);

        foreach (var message in messages)
        {
            stoppingToken.ThrowIfCancellationRequested();
            try
            {
                var payload = JsonSerializer.Deserialize<TestRunIngestedPayload>(message.PayloadJson)
                    ?? throw new InvalidOperationException("Empty outbox payload.");

                var runEvent = await testRunRepository.GetByIdAsync(payload.TestRunEventId)
                    ?? throw new InvalidOperationException($"TestRunEvent {payload.TestRunEventId} not found.");
                var repository = await repositoryRepository.GetByIdAsync(payload.RepositoryId)
                    ?? throw new InvalidOperationException($"Repository {payload.RepositoryId} not found.");

                await processor.ProcessAsync(repository, runEvent);
                await outboxRepository.MarkProcessedAsync(message.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process outbox message {MessageId}", message.Id);
                await outboxRepository.MarkFailedAsync(message.Id, ex.Message);
            }
        }

        return messages.Count > 0;
    }
}
