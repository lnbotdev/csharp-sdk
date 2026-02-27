using LnBot.Models;

namespace LnBot.Resources;

/// <summary>
/// Restore wallet access with passkey or recovery passphrase.
/// </summary>
public sealed class RestoreResource
{
    private readonly LnBotClient _client;
    internal RestoreResource(LnBotClient client) => _client = client;

    /// <summary>
    /// Restores wallet access by verifying the 12-word recovery passphrase.
    /// </summary>
    public Task<RecoveryRestoreResponse> RecoveryAsync(RecoveryRestoreRequest request, CancellationToken cancellationToken = default)
        => _client.PostAsync<RecoveryRestoreResponse>("/v1/restore/recovery", request, cancellationToken);

    /// <summary>
    /// Begins WebAuthn authentication to restore wallet access using a passkey.
    /// </summary>
    public Task<RestorePasskeyBeginResponse> PasskeyBeginAsync(CancellationToken cancellationToken = default)
        => _client.PostAsync<RestorePasskeyBeginResponse>("/v1/restore/passkey/begin", null, cancellationToken);

    /// <summary>
    /// Completes passkey restore by verifying the assertion.
    /// </summary>
    public Task<RestorePasskeyCompleteResponse> PasskeyCompleteAsync(RestorePasskeyCompleteRequest request, CancellationToken cancellationToken = default)
        => _client.PostAsync<RestorePasskeyCompleteResponse>("/v1/restore/passkey/complete", request, cancellationToken);
}
