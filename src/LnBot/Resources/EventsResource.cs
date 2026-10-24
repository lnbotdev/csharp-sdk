using System.Runtime.CompilerServices;
using System.Text.Json;
using LnBot.Models;

namespace LnBot.Resources;

/// <summary>
/// Wallet-scoped real-time event stream via SSE.
/// </summary>
public sealed class EventsResource
{
    private readonly LnBotClient _client;
    private readonly string _prefix;

    internal EventsResource(LnBotClient client, string prefix)
    {
        _client = client;
        _prefix = prefix;
    }

    /// <summary>
    /// Opens a Server-Sent Events stream for real-time wallet notifications.
    /// Events: <c>invoice.created</c>, <c>invoice.settled</c>, <c>payment.created</c>, <c>payment.settled</c>
    /// and <c>payment.failed</c>. See <see cref="WalletEvent.Data"/> for the payload.
    /// An event that can't be decoded is skipped.
    /// </summary>
    public async IAsyncEnumerable<WalletEvent> StreamAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using var stream = await _client.GetStreamAsync($"{_prefix}/events", cancellationToken).ConfigureAwait(false);

        await foreach (var message in ServerSentEvents.ReadAsync(stream, cancellationToken).ConfigureAwait(false))
        {
            if (!ServerSentEvents.TryDeserialize<WalletEvent>(message, out var walletEvent)) continue;
            yield return walletEvent;
        }
    }
}
