using System.Runtime.CompilerServices;
using System.Text.Json;
using LnBot.Models;

namespace LnBot.Resources;

/// <summary>
/// Invoice operations — create, list, get, and watch invoices.
/// </summary>
public sealed class InvoicesResource
{
    private readonly LnBotClient _client;
    internal InvoicesResource(LnBotClient client) => _client = client;

    /// <summary>
    /// Creates a BOLT11 invoice for the authenticated wallet.
    /// </summary>
    public Task<InvoiceResponse> CreateAsync(CreateInvoiceRequest request, CancellationToken cancellationToken = default)
        => _client.PostAsync<InvoiceResponse>("/v1/invoices", request, cancellationToken);

    /// <summary>
    /// Lists invoices in reverse chronological order.
    /// </summary>
    public Task<List<InvoiceResponse>> ListAsync(PaginationParams? pagination = null, CancellationToken cancellationToken = default)
        => _client.GetAsync<List<InvoiceResponse>>(BuildListPath(pagination), cancellationToken);

    /// <summary>
    /// Gets a specific invoice by number.
    /// </summary>
    public Task<InvoiceResponse> GetAsync(int number, CancellationToken cancellationToken = default)
        => _client.GetAsync<InvoiceResponse>($"/v1/invoices/{number}", cancellationToken);

    /// <summary>
    /// Gets a specific invoice by payment hash.
    /// </summary>
    public Task<InvoiceResponse> GetByHashAsync(string paymentHash, CancellationToken cancellationToken = default)
        => _client.GetAsync<InvoiceResponse>($"/v1/invoices/{Uri.EscapeDataString(paymentHash)}", cancellationToken);

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

    /// <summary>
    /// Opens an SSE stream that emits when the invoice settles or expires.
    /// </summary>
    public async IAsyncEnumerable<InvoiceEvent> WatchAsync(int number, int? timeout = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var path = $"/v1/invoices/{number}/events";
        if (timeout.HasValue) path += $"?timeout={timeout.Value}";

        await using var stream = await _client.GetStreamAsync(path, cancellationToken).ConfigureAwait(false);
        using var reader = new StreamReader(stream);

        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line is null) break;
            if (!line.StartsWith("data: ")) continue;

            var json = line["data: ".Length..];
            var evt = JsonSerializer.Deserialize<InvoiceEvent>(json, LnBotClient.GetJsonOptions());
            if (evt is not null) yield return evt;
        }
    }

    /// <summary>
    /// Opens an SSE stream by payment hash that emits when the invoice settles or expires.
    /// </summary>
    public async IAsyncEnumerable<InvoiceEvent> WatchByHashAsync(string paymentHash, int? timeout = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var path = $"/v1/invoices/{Uri.EscapeDataString(paymentHash)}/events";
        if (timeout.HasValue) path += $"?timeout={timeout.Value}";

        await using var stream = await _client.GetStreamAsync(path, cancellationToken).ConfigureAwait(false);
        using var reader = new StreamReader(stream);

        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line is null) break;
            if (!line.StartsWith("data: ")) continue;

            var json = line["data: ".Length..];
            var evt = JsonSerializer.Deserialize<InvoiceEvent>(json, LnBotClient.GetJsonOptions());
            if (evt is not null) yield return evt;
        }
    }

    private static string BuildListPath(PaginationParams? pagination)
    {
        if (pagination is null) return "/v1/invoices";
        var parts = new List<string>();
        if (pagination.Limit.HasValue) parts.Add($"limit={pagination.Limit.Value}");
        if (pagination.After.HasValue) parts.Add($"after={pagination.After.Value}");
        return parts.Count > 0 ? $"/v1/invoices?{string.Join('&', parts)}" : "/v1/invoices";
    }
}
