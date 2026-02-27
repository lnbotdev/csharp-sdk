using LnBot.Models;

namespace LnBot.Resources;

/// <summary>
/// Wallet management operations.
/// </summary>
public sealed class WalletsResource
{
    private readonly LnBotClient _client;
    internal WalletsResource(LnBotClient client) => _client = client;

    /// <summary>
    /// Creates a new wallet. No authentication required.
    /// </summary>
    public Task<CreateWalletResponse> CreateAsync(CreateWalletRequest? request = null, CancellationToken cancellationToken = default)
        => _client.PostAsync<CreateWalletResponse>("/v1/wallets", request, cancellationToken);

    /// <summary>
    /// Returns the authenticated wallet.
    /// </summary>
    public Task<WalletResponse> CurrentAsync(CancellationToken cancellationToken = default)
        => _client.GetAsync<WalletResponse>("/v1/wallets/current", cancellationToken);

    /// <summary>
    /// Updates the wallet name.
    /// </summary>
    public Task<WalletResponse> UpdateAsync(UpdateWalletRequest request, CancellationToken cancellationToken = default)
        => _client.PatchAsync<WalletResponse>("/v1/wallets/current", request, cancellationToken);
}
