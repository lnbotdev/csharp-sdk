using LnBot.Models;

namespace LnBot.Resources;

/// <summary>
/// Wallet-scoped key management (wk_ keys).
/// </summary>
public sealed class WalletKeyResource
{
    private readonly LnBotClient _client;
    private readonly string _prefix;

    internal WalletKeyResource(LnBotClient client, string prefix)
    {
        _client = client;
        _prefix = prefix;
    }

    /// <summary>
    /// Creates a wallet-scoped API key.
    /// </summary>
    public Task<WalletKeyResponse> CreateAsync(CancellationToken cancellationToken = default)
        => _client.PostAsync<WalletKeyResponse>($"{_prefix}/key", null, cancellationToken);

    /// <summary>
    /// Gets info about the current wallet key.
    /// </summary>
    public Task<WalletKeyInfoResponse> GetAsync(CancellationToken cancellationToken = default)
        => _client.GetAsync<WalletKeyInfoResponse>($"{_prefix}/key", cancellationToken);

    /// <summary>
    /// Deletes the wallet-scoped API key.
    /// </summary>
    public Task DeleteAsync(CancellationToken cancellationToken = default)
        => _client.DeleteAsync($"{_prefix}/key", cancellationToken);

    /// <summary>
    /// Rotates the wallet-scoped API key. The old key is immediately invalidated.
    /// </summary>
    public Task<WalletKeyResponse> RotateAsync(CancellationToken cancellationToken = default)
        => _client.PostAsync<WalletKeyResponse>($"{_prefix}/key/rotate", null, cancellationToken);
}
