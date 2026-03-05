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
    /// </summary>
    public async IAsyncEnumerable<WalletEvent> StreamAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using var stream = await _client.GetStreamAsync($"{_prefix}/events", cancellationToken).ConfigureAwait(false);
        using var reader = new StreamReader(stream);

        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line is null) break;
            if (!line.StartsWith("data: ")) continue;

            var json = line["data: ".Length..];
            WalletEvent? evt;
            try { evt = JsonSerializer.Deserialize<WalletEvent>(json, LnBotClient.GetJsonOptions()); }
            catch (JsonException) { continue; }
            if (evt is not null) yield return evt;
        }
    }
}
