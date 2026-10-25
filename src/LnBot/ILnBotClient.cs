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

    // The status calls were added in 1.2.0 with default implementations, so a class that implemented this
    // interface against an earlier version still compiles and loads. LnBotClient implements them.

    /// <summary>Returns the service's status right now. Public: needs no API key.</summary>
    Task<ServiceStatusResponse> GetStatusAsync(CancellationToken cancellationToken = default)
        => throw new NotSupportedException($"{GetType().Name} does not implement {nameof(GetStatusAsync)}.");

    /// <summary>Returns the service's status over time. Public: needs no API key.</summary>
    /// <param name="range"><c>24h</c> (default), <c>7d</c>, <c>30d</c> or <c>90d</c>; see <see cref="StatusRange"/>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<ServiceStatusHistoryResponse> GetStatusHistoryAsync(string range = StatusRange.Last24Hours, CancellationToken cancellationToken = default)
        => throw new NotSupportedException($"{GetType().Name} does not implement {nameof(GetStatusHistoryAsync)}.");

    /// <summary>Returns a wallet-scoped handle.</summary>
    WalletScope Wallet(string walletId);
}
