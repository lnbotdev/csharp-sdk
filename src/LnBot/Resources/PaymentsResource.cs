using System.Runtime.CompilerServices;
using System.Text.Json;
using LnBot.Models;

namespace LnBot.Resources;

/// <summary>
/// Payment operations — send sats and track payment status.
/// </summary>
public sealed class PaymentsResource
{
    private readonly LnBotClient _client;
    internal PaymentsResource(LnBotClient client) => _client = client;

    /// <summary>
    /// Sends sats to a Lightning address, LNURL, or BOLT11 invoice.
    /// </summary>
    public Task<PaymentResponse> CreateAsync(CreatePaymentRequest request, CancellationToken cancellationToken = default)
        => _client.PostAsync<PaymentResponse>("/v1/payments", request, cancellationToken);

    /// <summary>
    /// Lists payments in reverse chronological order.
    /// </summary>
    public Task<List<PaymentResponse>> ListAsync(PaginationParams? pagination = null, CancellationToken cancellationToken = default)
        => _client.GetAsync<List<PaymentResponse>>(BuildListPath(pagination), cancellationToken);

    /// <summary>
    /// Gets a specific payment by number.
    /// </summary>
    public Task<PaymentResponse> GetAsync(int number, CancellationToken cancellationToken = default)
        => _client.GetAsync<PaymentResponse>($"/v1/payments/{number}", cancellationToken);

    /// <summary>
    /// Gets a specific payment by payment hash.
    /// </summary>
    public Task<PaymentResponse> GetByHashAsync(string paymentHash, CancellationToken cancellationToken = default)
        => _client.GetAsync<PaymentResponse>($"/v1/payments/{Uri.EscapeDataString(paymentHash)}", cancellationToken);

    /// <summary>
    /// Opens an SSE stream that emits when the payment settles or fails.
    /// </summary>
    public async IAsyncEnumerable<PaymentEvent> WatchAsync(int number, int? timeout = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var path = $"/v1/payments/{number}/events";
        if (timeout.HasValue) path += $"?timeout={timeout.Value}";

        await using var stream = await _client.GetStreamAsync(path, cancellationToken).ConfigureAwait(false);
        using var reader = new StreamReader(stream);

        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line is null) break;
            if (!line.StartsWith("data: ")) continue;

            var json = line["data: ".Length..];
            var evt = JsonSerializer.Deserialize<PaymentEvent>(json, LnBotClient.GetJsonOptions());
            if (evt is not null) yield return evt;
        }
    }

    /// <summary>
    /// Opens an SSE stream by payment hash that emits when the payment settles or fails.
    /// </summary>
    public async IAsyncEnumerable<PaymentEvent> WatchByHashAsync(string paymentHash, int? timeout = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var path = $"/v1/payments/{Uri.EscapeDataString(paymentHash)}/events";
        if (timeout.HasValue) path += $"?timeout={timeout.Value}";

        await using var stream = await _client.GetStreamAsync(path, cancellationToken).ConfigureAwait(false);
        using var reader = new StreamReader(stream);

        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line is null) break;
            if (!line.StartsWith("data: ")) continue;

            var json = line["data: ".Length..];
            var evt = JsonSerializer.Deserialize<PaymentEvent>(json, LnBotClient.GetJsonOptions());
            if (evt is not null) yield return evt;
        }
    }

    private static string BuildListPath(PaginationParams? pagination)
    {
        if (pagination is null) return "/v1/payments";
        var parts = new List<string>();
        if (pagination.Limit.HasValue) parts.Add($"limit={pagination.Limit.Value}");
        if (pagination.After.HasValue) parts.Add($"after={pagination.After.Value}");
        return parts.Count > 0 ? $"/v1/payments?{string.Join('&', parts)}" : "/v1/payments";
    }
}
