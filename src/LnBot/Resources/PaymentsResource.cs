using System.Globalization;
using System.Runtime.CompilerServices;
using LnBot.Models;

namespace LnBot.Resources;

/// <summary>
/// Wallet-scoped payment operations — send sats and track payment status.
/// </summary>
public sealed class PaymentsResource
{
    private readonly LnBotClient _client;
    private readonly string _prefix;

    internal PaymentsResource(LnBotClient client, string prefix)
    {
        _client = client;
        _prefix = prefix;
    }

    /// <summary>
    /// Sends sats to a Lightning address, LNURL, or BOLT11 invoice.
    /// </summary>
    public Task<PaymentResponse> CreateAsync(CreatePaymentRequest request, CancellationToken cancellationToken = default)
        => _client.PostAsync<PaymentResponse>($"{_prefix}/payments", request, cancellationToken);

    /// <summary>
    /// Lists payments in reverse chronological order.
    /// </summary>
    public Task<List<PaymentResponse>> ListAsync(PaginationParams? pagination = null, CancellationToken cancellationToken = default)
        => _client.GetAsync<List<PaymentResponse>>(BuildListPath(pagination), cancellationToken);

    /// <summary>
    /// Gets a specific payment by number.
    /// </summary>
    public Task<PaymentResponse> GetAsync(int number, CancellationToken cancellationToken = default)
        => _client.GetAsync<PaymentResponse>($"{_prefix}/payments/{number}", cancellationToken);

    /// <summary>
    /// Gets a specific payment by payment hash.
    /// </summary>
    public Task<PaymentResponse> GetByHashAsync(string paymentHash, CancellationToken cancellationToken = default)
        => _client.GetAsync<PaymentResponse>($"{_prefix}/payments/{Uri.EscapeDataString(paymentHash)}", cancellationToken);

    /// <summary>
    /// Resolves a payment target without paying it: type, min/max amounts and pricing.
    /// A BOLT11 invoice is priced at its own amount. A Lightning address or LNURL returns
    /// <see cref="ResolveTargetResponse.MaxSendable"/>, the most this wallet can send there now.
    /// </summary>
    public Task<ResolveTargetResponse> ResolveAsync(string target, CancellationToken cancellationToken = default)
        => _client.GetAsync<ResolveTargetResponse>(BuildResolvePath(target, null), cancellationToken);

    /// <summary>
    /// Resolves a payment target and prices a payment of <paramref name="amount"/> sats without paying it:
    /// network fee estimate, service fee, reserved fee and total.
    /// Pricing a Lightning address or LNURL requests an invoice from the recipient, so call it once the amount is settled.
    /// </summary>
    /// <param name="target">Lightning address, LNURL, or BOLT11 invoice.</param>
    /// <param name="amount">Amount in sats to price. For a BOLT11 invoice it must equal the invoice amount.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task<ResolveTargetResponse> ResolveAsync(string target, long amount, CancellationToken cancellationToken = default)
        => _client.GetAsync<ResolveTargetResponse>(BuildResolvePath(target, amount), cancellationToken);

    /// <summary>
    /// Opens an SSE stream that emits when the payment settles or fails.
    /// If the wait runs out first (<paramref name="timeout"/> seconds, default 60, max 300), it emits a
    /// <c>timeout</c> event whose data is the payment's current state; read it again or watch again.
    /// An event that can't be decoded is skipped.
    /// </summary>
    public async IAsyncEnumerable<PaymentEvent> WatchAsync(int number, int? timeout = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var path = $"{_prefix}/payments/{number}/events";
        if (timeout.HasValue) path += $"?timeout={timeout.Value}";

        await using var stream = await _client.GetStreamAsync(path, cancellationToken).ConfigureAwait(false);

        await foreach (var message in ServerSentEvents.ReadAsync(stream, cancellationToken).ConfigureAwait(false))
        {
            if (!ServerSentEvents.TryDeserializeWatchEvent<PaymentResponse>(message, out var name, out var data)) continue;
            yield return new PaymentEvent { Event = name, Data = data };
        }
    }

    /// <summary>
    /// Opens an SSE stream by payment hash that emits when the payment settles or fails.
    /// If the wait runs out first (<paramref name="timeout"/> seconds, default 60, max 300), it emits a
    /// <c>timeout</c> event whose data is the payment's current state; read it again or watch again.
    /// An event that can't be decoded is skipped.
    /// </summary>
    public async IAsyncEnumerable<PaymentEvent> WatchByHashAsync(string paymentHash, int? timeout = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var path = $"{_prefix}/payments/{Uri.EscapeDataString(paymentHash)}/events";
        if (timeout.HasValue) path += $"?timeout={timeout.Value}";

        await using var stream = await _client.GetStreamAsync(path, cancellationToken).ConfigureAwait(false);

        await foreach (var message in ServerSentEvents.ReadAsync(stream, cancellationToken).ConfigureAwait(false))
        {
            if (!ServerSentEvents.TryDeserializeWatchEvent<PaymentResponse>(message, out var name, out var data)) continue;
            yield return new PaymentEvent { Event = name, Data = data };
        }
    }

    private string BuildResolvePath(string target, long? amount)
    {
        var path = $"{_prefix}/payments/resolve?target={Uri.EscapeDataString(target)}";
        if (amount.HasValue) path += $"&amount={amount.Value.ToString(CultureInfo.InvariantCulture)}";
        return path;
    }

    private string BuildListPath(PaginationParams? pagination)
    {
        var basePath = $"{_prefix}/payments";
        if (pagination is null) return basePath;
        var parts = new List<string>();
        if (pagination.Limit.HasValue) parts.Add($"limit={pagination.Limit.Value}");
        if (pagination.After.HasValue) parts.Add($"after={pagination.After.Value}");
        return parts.Count > 0 ? $"{basePath}?{string.Join('&', parts)}" : basePath;
    }
}
