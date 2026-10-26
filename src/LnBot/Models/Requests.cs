using System.Text.Json.Serialization;

namespace LnBot.Models;

public sealed class UpdateWalletRequest
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }
}

public sealed class CreateInvoiceRequest
{
    [JsonPropertyName("amount")]
    public required long Amount { get; set; }

    [JsonPropertyName("reference")]
    public string? Reference { get; set; }

    [JsonPropertyName("memo")]
    public string? Memo { get; set; }
}

public sealed class CreateInvoiceForWalletRequest
{
    [JsonPropertyName("walletId")]
    public required string WalletId { get; set; }

    [JsonPropertyName("amount")]
    public required long Amount { get; set; }

    [JsonPropertyName("reference")]
    public string? Reference { get; set; }

    [JsonPropertyName("comment")]
    public string? Comment { get; set; }
}

public sealed class CreateInvoiceForAddressRequest
{
    [JsonPropertyName("address")]
    public required string Address { get; set; }

    [JsonPropertyName("amount")]
    public required long Amount { get; set; }

    [JsonPropertyName("tag")]
    public string? Tag { get; set; }

    [JsonPropertyName("comment")]
    public string? Comment { get; set; }
}

public sealed class CreatePaymentRequest
{
    [JsonPropertyName("target")]
    public required string Target { get; set; }

    [JsonPropertyName("amount")]
    public long? Amount { get; set; }

    [JsonPropertyName("idempotencyKey")]
    public string? IdempotencyKey { get; set; }

    /// <summary>
    /// Maximum network fee in sats: the Lightning routing, on top of the 0.25% service fee. Leave null to use the
    /// server default (1% of the amount, minimum 20 sats); avoid hard-coding a small value, which rejects larger payments.
    /// Accepted range is 1 to max(10,000, 5% of the amount). Only the fee estimate plus a small buffer is held
    /// while the payment is in flight. Ignored for internal transfers.
    /// </summary>
    [JsonPropertyName("maxFee")]
    public long? MaxFee { get; set; }

    [JsonPropertyName("reference")]
    public string? Reference { get; set; }
}

public sealed class CreateAddressRequest
{
    [JsonPropertyName("address")]
    public string? Address { get; set; }
}

public sealed class TransferAddressRequest
{
    [JsonPropertyName("targetWalletKey")]
    public required string TargetWalletKey { get; set; }
}

public sealed class CreateWebhookRequest
{
    [JsonPropertyName("url")]
    public required string Url { get; set; }
}

public sealed class RecoveryRestoreRequest
{
    [JsonPropertyName("passphrase")]
    public required string Passphrase { get; set; }
}

public sealed class BackupPasskeyCompleteRequest
{
    [JsonPropertyName("sessionId")]
    public required string SessionId { get; set; }

    [JsonPropertyName("attestation")]
    public required System.Text.Json.JsonElement Attestation { get; set; }
}

public sealed class RestorePasskeyCompleteRequest
{
    [JsonPropertyName("sessionId")]
    public required string SessionId { get; set; }

    [JsonPropertyName("assertion")]
    public required System.Text.Json.JsonElement Assertion { get; set; }
}

public sealed class PaginationParams
{
    public int? Limit { get; set; }
    public int? After { get; set; }
}

// ---------------------------------------------------------------------------
// L402
// ---------------------------------------------------------------------------

public sealed class CreateL402ChallengeRequest
{
    [JsonPropertyName("amount")]
    public required long Amount { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("expirySeconds")]
    public int? ExpirySeconds { get; set; }

    [JsonPropertyName("caveats")]
    public List<string>? Caveats { get; set; }
}

public sealed class VerifyL402Request
{
    [JsonPropertyName("authorization")]
    public required string Authorization { get; set; }
}

public sealed class PayL402Request
{
    [JsonPropertyName("wwwAuthenticate")]
    public required string WwwAuthenticate { get; set; }

    /// <summary>
    /// Maximum network fee in sats: the Lightning routing, on top of the 0.25% service fee. Same default and limits as
    /// <see cref="CreatePaymentRequest.MaxFee"/>: leave null to use the server default (1% of the amount,
    /// minimum 20 sats).
    /// </summary>
    [JsonPropertyName("maxFee")]
    public long? MaxFee { get; set; }

    [JsonPropertyName("reference")]
    public string? Reference { get; set; }

    [JsonPropertyName("wait")]
    public bool? Wait { get; set; }

    [JsonPropertyName("timeout")]
    public int? Timeout { get; set; }
}
