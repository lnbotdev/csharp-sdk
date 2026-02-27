using LnBot.Models;

namespace LnBot.Resources;

/// <summary>
/// Webhook endpoint management.
/// </summary>
public sealed class WebhooksResource
{
    private readonly LnBotClient _client;
    internal WebhooksResource(LnBotClient client) => _client = client;

    /// <summary>
    /// Registers a URL to receive event callbacks. The signing secret is returned only once.
    /// </summary>
    public Task<CreateWebhookResponse> CreateAsync(CreateWebhookRequest request, CancellationToken cancellationToken = default)
        => _client.PostAsync<CreateWebhookResponse>("/v1/webhooks", request, cancellationToken);

    /// <summary>
    /// Lists all webhook endpoints for the authenticated wallet.
    /// </summary>
    public Task<List<WebhookResponse>> ListAsync(CancellationToken cancellationToken = default)
        => _client.GetAsync<List<WebhookResponse>>("/v1/webhooks", cancellationToken);

    /// <summary>
    /// Deletes a webhook endpoint by ID.
    /// </summary>
    public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
        => _client.DeleteAsync($"/v1/webhooks/{Uri.EscapeDataString(id)}", cancellationToken);
}
