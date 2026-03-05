using LnBot.Models;

namespace LnBot.Resources;

/// <summary>
/// Wallet-scoped Lightning address management.
/// </summary>
public sealed class AddressesResource
{
    private readonly LnBotClient _client;
    private readonly string _prefix;

    internal AddressesResource(LnBotClient client, string prefix)
    {
        _client = client;
        _prefix = prefix;
    }

    /// <summary>
    /// Creates a random or vanity Lightning address.
    /// </summary>
    public Task<AddressResponse> CreateAsync(CreateAddressRequest? request = null, CancellationToken cancellationToken = default)
        => _client.PostAsync<AddressResponse>($"{_prefix}/addresses", request, cancellationToken);

    /// <summary>
    /// Lists all addresses belonging to this wallet.
    /// </summary>
    public Task<List<AddressResponse>> ListAsync(CancellationToken cancellationToken = default)
        => _client.GetAsync<List<AddressResponse>>($"{_prefix}/addresses", cancellationToken);

    /// <summary>
    /// Deletes an address.
    /// </summary>
    public Task DeleteAsync(string address, CancellationToken cancellationToken = default)
        => _client.DeleteAsync($"{_prefix}/addresses/{Uri.EscapeDataString(address)}", cancellationToken);

    /// <summary>
    /// Transfers an address to another wallet.
    /// </summary>
    public Task<TransferAddressResponse> TransferAsync(string address, TransferAddressRequest request, CancellationToken cancellationToken = default)
        => _client.PostAsync<TransferAddressResponse>($"{_prefix}/addresses/{Uri.EscapeDataString(address)}/transfer", request, cancellationToken);
}
