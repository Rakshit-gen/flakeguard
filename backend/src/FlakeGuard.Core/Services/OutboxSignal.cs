using System.Threading.Channels;

namespace FlakeGuard.Core.Services;

/// <summary>
/// Wakes the outbox worker immediately after a write instead of waiting for its periodic poll.
/// The outbox table is the source of truth; this channel is a latency optimization only and
/// is safe to lose signals from (a missed wake-up is caught by the next poll).
/// </summary>
public class OutboxSignal
{
    private readonly Channel<bool> _channel = Channel.CreateBounded<bool>(
        new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropWrite });

    public void NotifyWritten() => _channel.Writer.TryWrite(true);

    public async Task WaitAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);
        try
        {
            await _channel.Reader.ReadAsync(cts.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Fallback poll interval elapsed with no signal; caller re-polls the outbox table.
        }
    }
}
