using LnBot.Models;

namespace LnBot.Resources;

/// <summary>
/// API key management.
/// </summary>
public sealed class KeysResource
{
    private readonly LnBotClient _client;
    internal KeysResource(LnBotClient client) => _client = client;

    /// <summary>
    /// Lists all API keys (metadata only, not the key values).
    /// </summary>
    public Task<List<ApiKeyResponse>> ListAsync(CancellationToken cancellationToken = default)
        => _client.GetAsync<List<ApiKeyResponse>>("/v1/keys", cancellationToken);

    /// <summary>
    /// Rotates an API key. The old key is immediately invalidated.
    /// </summary>
    /// <param name="slot">Key slot: 0 = primary, 1 = secondary.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task<RotateApiKeyResponse> RotateAsync(int slot, CancellationToken cancellationToken = default)
        => _client.PostAsync<RotateApiKeyResponse>($"/v1/keys/{slot}/rotate", null, cancellationToken);
}
