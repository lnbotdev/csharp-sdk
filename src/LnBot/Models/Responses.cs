using System.Text.Json.Serialization;

namespace LnBot.Models;

// ---------------------------------------------------------------------------
// Account
// ---------------------------------------------------------------------------

public sealed class RegisterResponse
{
    [JsonPropertyName("userId")]
    public required string UserId { get; init; }

    [JsonPropertyName("primaryKey")]
    public required string PrimaryKey { get; init; }

    [JsonPropertyName("secondaryKey")]
    public required string SecondaryKey { get; init; }

    [JsonPropertyName("recoveryPassphrase")]
    public required string RecoveryPassphrase { get; init; }
}

public sealed class MeResponse
{
    [JsonPropertyName("walletId")]
    public string? WalletId { get; init; }
}

// ---------------------------------------------------------------------------
// Wallets
// ---------------------------------------------------------------------------

public sealed class WalletResponse
{
    [JsonPropertyName("walletId")]
    public required string WalletId { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("balance")]
    public required long Balance { get; init; }

    [JsonPropertyName("onHold")]
    public required long OnHold { get; init; }

    [JsonPropertyName("available")]
    public required long Available { get; init; }
}

public sealed class CreateWalletResponse
{
    [JsonPropertyName("walletId")]
    public required string WalletId { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("address")]
    public required string Address { get; init; }
}

public sealed class WalletListItem
{
    [JsonPropertyName("walletId")]
    public required string WalletId { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("createdAt")]
    public DateTimeOffset? CreatedAt { get; init; }
}

// ---------------------------------------------------------------------------
// Wallet Key
// ---------------------------------------------------------------------------

public sealed class WalletKeyResponse
{
    [JsonPropertyName("key")]
    public required string Key { get; init; }

    [JsonPropertyName("hint")]
    public required string Hint { get; init; }
}

public sealed class WalletKeyInfoResponse
{
    [JsonPropertyName("hint")]
    public required string Hint { get; init; }

    [JsonPropertyName("createdAt")]
    public DateTimeOffset? CreatedAt { get; init; }

    [JsonPropertyName("lastUsedAt")]
    public DateTimeOffset? LastUsedAt { get; init; }
}

// ---------------------------------------------------------------------------
// Invoices
// ---------------------------------------------------------------------------

public sealed class InvoiceResponse
{
    [JsonPropertyName("number")]
    public required int Number { get; init; }

    [JsonPropertyName("status")]
    public required InvoiceStatus Status { get; init; }

    [JsonPropertyName("amount")]
    public required long Amount { get; init; }

    [JsonPropertyName("bolt11")]
    public required string Bolt11 { get; init; }

    [JsonPropertyName("reference")]
    public string? Reference { get; init; }

    [JsonPropertyName("memo")]
    public string? Memo { get; init; }

    [JsonPropertyName("preimage")]
    public string? Preimage { get; init; }

    [JsonPropertyName("txNumber")]
    public int? TxNumber { get; init; }

    [JsonPropertyName("createdAt")]
    public DateTimeOffset? CreatedAt { get; init; }

    [JsonPropertyName("settledAt")]
    public DateTimeOffset? SettledAt { get; init; }

    [JsonPropertyName("expiresAt")]
    public DateTimeOffset? ExpiresAt { get; init; }
}

public sealed class AddressInvoiceResponse
{
    [JsonPropertyName("bolt11")]
    public required string Bolt11 { get; init; }

    [JsonPropertyName("amount")]
    public required long Amount { get; init; }

    [JsonPropertyName("expiresAt")]
    public required DateTimeOffset ExpiresAt { get; init; }
}

// ---------------------------------------------------------------------------
// Payments
// ---------------------------------------------------------------------------

public sealed class PaymentResponse
{
    [JsonPropertyName("number")]
    public required int Number { get; init; }

    [JsonPropertyName("status")]
    public required PaymentStatus Status { get; init; }

    [JsonPropertyName("amount")]
    public required long Amount { get; init; }

    /// <summary>
    /// Network fee reserved for this payment in sats: the fee estimate plus a small buffer, or the max fee when
    /// no estimate was available. Never above <see cref="CreatePaymentRequest.MaxFee"/>. The unused part is
    /// released at settlement.
    /// </summary>
    [JsonPropertyName("maxFee")]
    public required long MaxFee { get; init; }

    /// <summary>Service fee in sats: 0.25%, minimum 1 sat. 0 for internal transfers.</summary>
    [JsonPropertyName("serviceFee")]
    public required long ServiceFee { get; init; }

    /// <summary>
    /// Network fee actually charged in sats (Spark's fee plus Lightning routing). Null until settled.
    /// On success the wallet is debited <see cref="Amount"/> + <see cref="ActualFee"/> + <see cref="ServiceFee"/>.
    /// </summary>
    [JsonPropertyName("actualFee")]
    public long? ActualFee { get; init; }

    [JsonPropertyName("address")]
    public required string Address { get; init; }

    [JsonPropertyName("reference")]
    public string? Reference { get; init; }

    [JsonPropertyName("preimage")]
    public string? Preimage { get; init; }

    [JsonPropertyName("txNumber")]
    public int? TxNumber { get; init; }

    [JsonPropertyName("failureReason")]
    public string? FailureReason { get; init; }

    [JsonPropertyName("createdAt")]
    public DateTimeOffset? CreatedAt { get; init; }

    [JsonPropertyName("settledAt")]
    public DateTimeOffset? SettledAt { get; init; }
}

public sealed class ResolveTargetResponse
{
    /// <summary>Target type: <c>lightning_address</c>, <c>lnurl</c> or <c>bolt11</c>.</summary>
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    /// <summary>Minimum accepted amount in sats. Null for BOLT11 invoices.</summary>
    [JsonPropertyName("min")]
    public long? Min { get; init; }

    /// <summary>Maximum accepted amount in sats. Null for BOLT11 invoices.</summary>
    [JsonPropertyName("max")]
    public long? Max { get; init; }

    /// <summary>True if the recipient expects a fixed amount.</summary>
    [JsonPropertyName("fixed")]
    public bool? Fixed { get; init; }

    /// <summary>
    /// The amount priced in sats: the invoice amount for a BOLT11 invoice, otherwise the amount passed.
    /// Null when no amount was given.
    /// </summary>
    [JsonPropertyName("amount")]
    public long? Amount { get; init; }

    /// <summary>True when the recipient is a wallet on ln.bot: a free internal transfer.</summary>
    [JsonPropertyName("internal")]
    public bool Internal { get; init; }

    /// <summary>
    /// Estimated network fee in sats (Spark's 0.25% plus Lightning routing).
    /// Null without an amount or when no estimate is available. 0 for internal transfers.
    /// </summary>
    [JsonPropertyName("networkFeeEstimate")]
    public long? NetworkFeeEstimate { get; init; }

    /// <summary>Service fee in sats: 0.25%, minimum 1 sat. 0 for internal transfers. Null without an amount.</summary>
    [JsonPropertyName("serviceFee")]
    public long? ServiceFee { get; init; }

    /// <summary>
    /// Network fee in sats that would be reserved with the default max fee.
    /// Null without an amount, or when <see cref="FeeWarning"/> is set.
    /// </summary>
    [JsonPropertyName("maxFee")]
    public long? MaxFee { get; init; }

    /// <summary>
    /// Expected total cost in sats: <see cref="Amount"/> + <see cref="NetworkFeeEstimate"/> + <see cref="ServiceFee"/>.
    /// Null without an amount.
    /// </summary>
    [JsonPropertyName("total")]
    public long? Total { get; init; }

    /// <summary>
    /// For a Lightning address or LNURL resolved without an amount: the most this wallet can send there now,
    /// after fees. 0 if the wallet can't cover the recipient's minimum. Null otherwise.
    /// </summary>
    [JsonPropertyName("maxSendable")]
    public long? MaxSendable { get; init; }

    /// <summary>
    /// Set when sending without a max fee would be rejected because the fee is unusually high.
    /// The resolve call itself still succeeds.
    /// </summary>
    [JsonPropertyName("feeWarning")]
    public string? FeeWarning { get; init; }
}

// ---------------------------------------------------------------------------
// Addresses
// ---------------------------------------------------------------------------

public sealed class AddressResponse
{
    [JsonPropertyName("address")]
    public required string Address { get; init; }

    [JsonPropertyName("generated")]
    public required bool Generated { get; init; }

    [JsonPropertyName("cost")]
    public required long Cost { get; init; }

    [JsonPropertyName("createdAt")]
    public DateTimeOffset? CreatedAt { get; init; }
}

public sealed class TransferAddressResponse
{
    [JsonPropertyName("address")]
    public required string Address { get; init; }

    [JsonPropertyName("transferredTo")]
    public required string TransferredTo { get; init; }
}

// ---------------------------------------------------------------------------
// Transactions
// ---------------------------------------------------------------------------

public sealed class TransactionResponse
{
    [JsonPropertyName("number")]
    public required int Number { get; init; }

    [JsonPropertyName("type")]
    public required TransactionType Type { get; init; }

    [JsonPropertyName("amount")]
    public required long Amount { get; init; }

    [JsonPropertyName("balanceAfter")]
    public required long BalanceAfter { get; init; }

    [JsonPropertyName("networkFee")]
    public required long NetworkFee { get; init; }

    [JsonPropertyName("serviceFee")]
    public required long ServiceFee { get; init; }

    [JsonPropertyName("paymentHash")]
    public string? PaymentHash { get; init; }

    [JsonPropertyName("preimage")]
    public string? Preimage { get; init; }

    [JsonPropertyName("reference")]
    public string? Reference { get; init; }

    [JsonPropertyName("note")]
    public string? Note { get; init; }

    [JsonPropertyName("createdAt")]
    public DateTimeOffset? CreatedAt { get; init; }
}

// ---------------------------------------------------------------------------
// Keys (account-level)
// ---------------------------------------------------------------------------

public sealed class RotateApiKeyResponse
{
    [JsonPropertyName("key")]
    public required string Key { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }
}

// ---------------------------------------------------------------------------
// Webhooks
// ---------------------------------------------------------------------------

public sealed class WebhookResponse
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("url")]
    public required string Url { get; init; }

    [JsonPropertyName("active")]
    public required bool Active { get; init; }

    [JsonPropertyName("createdAt")]
    public DateTimeOffset? CreatedAt { get; init; }
}

public sealed class CreateWebhookResponse
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("url")]
    public required string Url { get; init; }

    [JsonPropertyName("secret")]
    public required string Secret { get; init; }

    [JsonPropertyName("createdAt")]
    public DateTimeOffset? CreatedAt { get; init; }
}

// ---------------------------------------------------------------------------
// Backup / Restore
// ---------------------------------------------------------------------------

public sealed class RecoveryBackupResponse
{
    [JsonPropertyName("passphrase")]
    public required string Passphrase { get; init; }
}

public sealed class RecoveryRestoreResponse
{
    [JsonPropertyName("walletId")]
    public required string WalletId { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("primaryKey")]
    public required string PrimaryKey { get; init; }

    [JsonPropertyName("secondaryKey")]
    public required string SecondaryKey { get; init; }
}

public sealed class BackupPasskeyBeginResponse
{
    [JsonPropertyName("sessionId")]
    public required string SessionId { get; init; }

    [JsonPropertyName("options")]
    public required System.Text.Json.JsonElement Options { get; init; }
}

public sealed class RestorePasskeyBeginResponse
{
    [JsonPropertyName("sessionId")]
    public required string SessionId { get; init; }

    [JsonPropertyName("options")]
    public required System.Text.Json.JsonElement Options { get; init; }
}

public sealed class RestorePasskeyCompleteResponse
{
    [JsonPropertyName("walletId")]
    public required string WalletId { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("primaryKey")]
    public required string PrimaryKey { get; init; }

    [JsonPropertyName("secondaryKey")]
    public required string SecondaryKey { get; init; }
}

// ---------------------------------------------------------------------------
// L402
// ---------------------------------------------------------------------------

public sealed class L402ChallengeResponse
{
    [JsonPropertyName("macaroon")]
    public required string Macaroon { get; init; }

    [JsonPropertyName("invoice")]
    public required string Invoice { get; init; }

    [JsonPropertyName("paymentHash")]
    public required string PaymentHash { get; init; }

    [JsonPropertyName("expiresAt")]
    public required DateTimeOffset ExpiresAt { get; init; }

    [JsonPropertyName("wwwAuthenticate")]
    public required string WwwAuthenticate { get; init; }
}

public sealed class VerifyL402Response
{
    [JsonPropertyName("valid")]
    public required bool Valid { get; init; }

    [JsonPropertyName("paymentHash")]
    public string? PaymentHash { get; init; }

    [JsonPropertyName("caveats")]
    public List<string>? Caveats { get; init; }

    [JsonPropertyName("error")]
    public string? Error { get; init; }
}

public sealed class L402PayResponse
{
    [JsonPropertyName("authorization")]
    public string? Authorization { get; init; }

    [JsonPropertyName("paymentHash")]
    public required string PaymentHash { get; init; }

    [JsonPropertyName("preimage")]
    public string? Preimage { get; init; }

    [JsonPropertyName("amount")]
    public required long Amount { get; init; }

    /// <summary>Network fee actually charged in sats (Spark's fee plus Lightning routing). Null until settled.</summary>
    [JsonPropertyName("fee")]
    public long? Fee { get; init; }

    [JsonPropertyName("paymentNumber")]
    public required int PaymentNumber { get; init; }

    [JsonPropertyName("status")]
    public required string Status { get; init; }
}
