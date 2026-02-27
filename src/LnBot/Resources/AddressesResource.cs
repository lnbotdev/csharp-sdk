using LnBot.Models;

namespace LnBot.Resources;

/// <summary>
/// Lightning address management.
/// </summary>
public sealed class AddressesResource
{
    private readonly LnBotClient _client;
    internal AddressesResource(LnBotClient client) => _client = client;

    /// <summary>
    /// Creates a random or vanity Lightning address.
    /// </summary>
    public Task<AddressResponse> CreateAsync(CreateAddressRequest? request = null, CancellationToken cancellationToken = default)
        => _client.PostAsync<AddressResponse>("/v1/addresses", request, cancellationToken);

    /// <summary>
    /// Lists all addresses belonging to the authenticated wallet.
    /// </summary>
    public Task<List<AddressResponse>> ListAsync(CancellationToken cancellationToken = default)
        => _client.GetAsync<List<AddressResponse>>("/v1/addresses", cancellationToken);

    /// <summary>
    /// Deletes an address.
    /// </summary>
    public Task DeleteAsync(string address, CancellationToken cancellationToken = default)
        => _client.DeleteAsync($"/v1/addresses/{Uri.EscapeDataString(address)}", cancellationToken);

    /// <summary>
    /// Transfers an address to another wallet.
    /// </summary>
    public Task<TransferAddressResponse> TransferAsync(string address, TransferAddressRequest request, CancellationToken cancellationToken = default)
        => _client.PostAsync<TransferAddressResponse>($"/v1/addresses/{Uri.EscapeDataString(address)}/transfer", request, cancellationToken);
}
