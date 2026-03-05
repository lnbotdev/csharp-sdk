using LnBot.Models;
using LnBot.Resources;

namespace LnBot;

/// <summary>
/// Interface for the LnBot API client. Register in DI as a singleton or scoped service.
/// </summary>
public interface ILnBotClient : IDisposable
{
    /// <summary>Account-level wallet management (create, list).</summary>
    WalletsResource Wallets { get; }

    /// <summary>Account-level API key management (uk_ keys).</summary>
    KeysResource Keys { get; }

    /// <summary>Public invoice creation (no auth required).</summary>
    PublicInvoicesResource Invoices { get; }

    /// <summary>Backup wallet access.</summary>
    BackupResource Backup { get; }

    /// <summary>Restore wallet access.</summary>
    RestoreResource Restore { get; }

    /// <summary>Registers a new account.</summary>
    Task<RegisterResponse> RegisterAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns the authenticated identity.</summary>
    Task<MeResponse> MeAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns a wallet-scoped handle.</summary>
    WalletScope Wallet(string walletId);
}
