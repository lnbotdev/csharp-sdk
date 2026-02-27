using System.Runtime.CompilerServices;
using System.Text.Json;
using LnBot.Models;

namespace LnBot.Resources;

/// <summary>
/// Real-time wallet event stream via SSE.
/// </summary>
public sealed class EventsResource
{
    private readonly LnBotClient _client;
    internal EventsResource(LnBotClient client) => _client = client;

    /// <summary>
    /// Opens a Server-Sent Events stream for real-time wallet notifications.
    /// Events: invoice.created, invoice.settled, payment.created, payment.settled, payment.failed.
    /// </summary>
    public async IAsyncEnumerable<WalletEvent> StreamAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using var stream = await _client.GetStreamAsync("/v1/events", cancellationToken).ConfigureAwait(false);
        using var reader = new StreamReader(stream);

        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line is null) break;
            if (!line.StartsWith("data: ")) continue;

            var json = line["data: ".Length..];
            var evt = JsonSerializer.Deserialize<WalletEvent>(json, LnBotClient.GetJsonOptions());
            if (evt is not null) yield return evt;
        }
    }
}
