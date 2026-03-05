using LnBot.Models;

namespace LnBot.Resources;

/// <summary>
/// Account-level wallet management: create and list wallets.
/// </summary>
public sealed class WalletsResource
{
    private readonly LnBotClient _client;
    internal WalletsResource(LnBotClient client) => _client = client;

    /// <summary>
    /// Creates a new wallet under the authenticated account.
    /// </summary>
    public Task<CreateWalletResponse> CreateAsync(CancellationToken cancellationToken = default)
        => _client.PostAsync<CreateWalletResponse>("/v1/wallets", null, cancellationToken);

    /// <summary>
    /// Lists all wallets belonging to the authenticated account.
    /// </summary>
    public Task<List<WalletListItem>> ListAsync(CancellationToken cancellationToken = default)
        => _client.GetAsync<List<WalletListItem>>("/v1/wallets", cancellationToken);
}
