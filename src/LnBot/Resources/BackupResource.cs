using LnBot.Models;

namespace LnBot.Resources;

/// <summary>
/// Backup wallet access with passkey or recovery passphrase.
/// </summary>
public sealed class BackupResource
{
    private readonly LnBotClient _client;
    internal BackupResource(LnBotClient client) => _client = client;

    /// <summary>
    /// Generates a 12-word BIP-39 recovery passphrase.
    /// </summary>
    public Task<RecoveryBackupResponse> RecoveryAsync(CancellationToken cancellationToken = default)
        => _client.PostAsync<RecoveryBackupResponse>("/v1/backup/recovery", null, cancellationToken);

    /// <summary>
    /// Begins WebAuthn registration to back up wallet access with a passkey.
    /// </summary>
    public Task<BackupPasskeyBeginResponse> PasskeyBeginAsync(CancellationToken cancellationToken = default)
        => _client.PostAsync<BackupPasskeyBeginResponse>("/v1/backup/passkey/begin", null, cancellationToken);

    /// <summary>
    /// Completes passkey backup by verifying the attestation.
    /// </summary>
    public Task PasskeyCompleteAsync(BackupPasskeyCompleteRequest request, CancellationToken cancellationToken = default)
        => _client.PostAsync("/v1/backup/passkey/complete", request, cancellationToken);
}
