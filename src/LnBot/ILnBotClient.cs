using LnBot.Resources;

namespace LnBot;

/// <summary>
/// Interface for the LnBot API client. Register in DI as a singleton or scoped service.
/// </summary>
public interface ILnBotClient : IDisposable
{
    /// <summary>Wallet management.</summary>
    WalletsResource Wallets { get; }

    /// <summary>Create and query invoices.</summary>
    InvoicesResource Invoices { get; }

    /// <summary>Send payments.</summary>
    PaymentsResource Payments { get; }

    /// <summary>Lightning address management.</summary>
    AddressesResource Addresses { get; }

    /// <summary>Transaction history.</summary>
    TransactionsResource Transactions { get; }

    /// <summary>Webhook endpoints.</summary>
    WebhooksResource Webhooks { get; }

    /// <summary>API key management.</summary>
    KeysResource Keys { get; }

    /// <summary>Real-time event stream.</summary>
    EventsResource Events { get; }

    /// <summary>Backup wallet access.</summary>
    BackupResource Backup { get; }

    /// <summary>Restore wallet access.</summary>
    RestoreResource Restore { get; }
}
