using LnBot.Models;

namespace LnBot.Resources;

/// <summary>
/// Wallet-scoped webhook endpoint management.
/// </summary>
public sealed class WebhooksResource
{
    private readonly LnBotClient _client;
    private readonly string _prefix;

    internal WebhooksResource(LnBotClient client, string prefix)
    {
        _client = client;
        _prefix = prefix;
    }

    /// <summary>
    /// Registers a URL to receive event callbacks. The signing secret is returned only once.
    /// </summary>
    public Task<CreateWebhookResponse> CreateAsync(CreateWebhookRequest request, CancellationToken cancellationToken = default)
        => _client.PostAsync<CreateWebhookResponse>($"{_prefix}/webhooks", request, cancellationToken);

    /// <summary>
    /// Lists all webhook endpoints for this wallet.
    /// </summary>
    public Task<List<WebhookResponse>> ListAsync(CancellationToken cancellationToken = default)
        => _client.GetAsync<List<WebhookResponse>>($"{_prefix}/webhooks", cancellationToken);

    /// <summary>
    /// Deletes a webhook endpoint by ID.
    /// </summary>
    public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
        => _client.DeleteAsync($"{_prefix}/webhooks/{Uri.EscapeDataString(id)}", cancellationToken);
}
