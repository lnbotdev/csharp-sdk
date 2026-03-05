using System.Runtime.CompilerServices;
using System.Text.Json;
using LnBot.Models;

namespace LnBot.Resources;

/// <summary>
/// Wallet-scoped invoice operations — create, list, get, and watch invoices.
/// </summary>
public sealed class InvoicesResource
{
    private readonly LnBotClient _client;
    private readonly string _prefix;

    internal InvoicesResource(LnBotClient client, string prefix)
    {
        _client = client;
        _prefix = prefix;
    }

    /// <summary>
    /// Creates a BOLT11 invoice.
    /// </summary>
    public Task<InvoiceResponse> CreateAsync(CreateInvoiceRequest request, CancellationToken cancellationToken = default)
        => _client.PostAsync<InvoiceResponse>($"{_prefix}/invoices", request, cancellationToken);

    /// <summary>
    /// Lists invoices in reverse chronological order.
    /// </summary>
    public Task<List<InvoiceResponse>> ListAsync(PaginationParams? pagination = null, CancellationToken cancellationToken = default)
        => _client.GetAsync<List<InvoiceResponse>>(BuildListPath(pagination), cancellationToken);

    /// <summary>
    /// Gets a specific invoice by number.
    /// </summary>
    public Task<InvoiceResponse> GetAsync(int number, CancellationToken cancellationToken = default)
        => _client.GetAsync<InvoiceResponse>($"{_prefix}/invoices/{number}", cancellationToken);

    /// <summary>
    /// Gets a specific invoice by payment hash.
    /// </summary>
    public Task<InvoiceResponse> GetByHashAsync(string paymentHash, CancellationToken cancellationToken = default)
        => _client.GetAsync<InvoiceResponse>($"{_prefix}/invoices/{Uri.EscapeDataString(paymentHash)}", cancellationToken);

    /// <summary>
    /// Opens an SSE stream that emits when the invoice settles or expires.
    /// </summary>
    public async IAsyncEnumerable<InvoiceEvent> WatchAsync(int number, int? timeout = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var path = $"{_prefix}/invoices/{number}/events";
        if (timeout.HasValue) path += $"?timeout={timeout.Value}";

        await using var stream = await _client.GetStreamAsync(path, cancellationToken).ConfigureAwait(false);
        using var reader = new StreamReader(stream);

        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line is null) break;
            if (!line.StartsWith("data: ")) continue;

            var json = line["data: ".Length..];
            InvoiceEvent? evt;
            try { evt = JsonSerializer.Deserialize<InvoiceEvent>(json, LnBotClient.GetJsonOptions()); }
            catch (JsonException) { continue; }
            if (evt is not null) yield return evt;
        }
    }

    /// <summary>
    /// Opens an SSE stream by payment hash that emits when the invoice settles or expires.
    /// </summary>
    public async IAsyncEnumerable<InvoiceEvent> WatchByHashAsync(string paymentHash, int? timeout = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var path = $"{_prefix}/invoices/{Uri.EscapeDataString(paymentHash)}/events";
        if (timeout.HasValue) path += $"?timeout={timeout.Value}";

        await using var stream = await _client.GetStreamAsync(path, cancellationToken).ConfigureAwait(false);
        using var reader = new StreamReader(stream);

        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line is null) break;
            if (!line.StartsWith("data: ")) continue;

            var json = line["data: ".Length..];
            InvoiceEvent? evt;
            try { evt = JsonSerializer.Deserialize<InvoiceEvent>(json, LnBotClient.GetJsonOptions()); }
            catch (JsonException) { continue; }
            if (evt is not null) yield return evt;
        }
    }

    private string BuildListPath(PaginationParams? pagination)
    {
        var basePath = $"{_prefix}/invoices";
        if (pagination is null) return basePath;
        var parts = new List<string>();
        if (pagination.Limit.HasValue) parts.Add($"limit={pagination.Limit.Value}");
        if (pagination.After.HasValue) parts.Add($"after={pagination.After.Value}");
        return parts.Count > 0 ? $"{basePath}?{string.Join('&', parts)}" : basePath;
    }
}

/// <summary>
/// Public (unauthenticated) invoice creation endpoints.
/// </summary>
public sealed class PublicInvoicesResource
{
    private readonly LnBotClient _client;
    internal PublicInvoicesResource(LnBotClient client) => _client = client;

    /// <summary>
    /// Creates an invoice for a specific wallet ID. No authentication required.
    /// </summary>
    public Task<AddressInvoiceResponse> CreateForWalletAsync(CreateInvoiceForWalletRequest request, CancellationToken cancellationToken = default)
        => _client.PostAsync<AddressInvoiceResponse>("/v1/invoices/for-wallet", request, cancellationToken);

    /// <summary>
    /// Creates an invoice for a Lightning address. No authentication required.
    /// </summary>
    public Task<AddressInvoiceResponse> CreateForAddressAsync(CreateInvoiceForAddressRequest request, CancellationToken cancellationToken = default)
        => _client.PostAsync<AddressInvoiceResponse>("/v1/invoices/for-address", request, cancellationToken);
}
