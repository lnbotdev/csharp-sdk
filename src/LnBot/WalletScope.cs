using LnBot.Models;
using LnBot.Resources;

namespace LnBot;

/// <summary>
/// A wallet-scoped handle that provides access to all wallet resources.
/// Obtain via <see cref="LnBotClient.Wallet(string)"/>.
/// </summary>
public sealed class WalletScope
{
    private readonly LnBotClient _client;
    private readonly string _prefix;

    /// <summary>The wallet ID this scope targets.</summary>
    public string WalletId { get; }

    /// <summary>Wallet-scoped API key management (wk_ keys).</summary>
    public WalletKeyResource Key { get; }

    /// <summary>Create and query invoices.</summary>
    public InvoicesResource Invoices { get; }

    /// <summary>Send payments.</summary>
    public PaymentsResource Payments { get; }

    /// <summary>Lightning address management.</summary>
    public AddressesResource Addresses { get; }

    /// <summary>Transaction history.</summary>
    public TransactionsResource Transactions { get; }

    /// <summary>Webhook endpoints.</summary>
    public WebhooksResource Webhooks { get; }

    /// <summary>Real-time event stream.</summary>
    public EventsResource Events { get; }

    /// <summary>L402 paywall authentication.</summary>
    public L402Resource L402 { get; }

    internal WalletScope(LnBotClient client, string walletId)
    {
        _client = client;
        _prefix = $"/v1/wallets/{Uri.EscapeDataString(walletId)}";
        WalletId = walletId;

        Key = new WalletKeyResource(client, _prefix);
        Invoices = new InvoicesResource(client, _prefix);
        Payments = new PaymentsResource(client, _prefix);
        Addresses = new AddressesResource(client, _prefix);
        Transactions = new TransactionsResource(client, _prefix);
        Webhooks = new WebhooksResource(client, _prefix);
        Events = new EventsResource(client, _prefix);
        L402 = new L402Resource(client, _prefix);
    }

    /// <summary>
    /// Returns the wallet details (balance, name, etc.).
    /// </summary>
    public Task<WalletResponse> GetAsync(CancellationToken cancellationToken = default)
        => _client.GetAsync<WalletResponse>(_prefix, cancellationToken);

    /// <summary>
    /// Updates the wallet (e.g. rename).
    /// </summary>
    public Task<WalletResponse> UpdateAsync(UpdateWalletRequest request, CancellationToken cancellationToken = default)
        => _client.PatchAsync<WalletResponse>(_prefix, request, cancellationToken);
}
