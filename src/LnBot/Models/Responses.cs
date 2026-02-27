using System.Text.Json.Serialization;

namespace LnBot.Models;

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

    [JsonPropertyName("primaryKey")]
    public required string PrimaryKey { get; init; }

    [JsonPropertyName("secondaryKey")]
    public required string SecondaryKey { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("address")]
    public required string Address { get; init; }

    [JsonPropertyName("recoveryPassphrase")]
    public required string RecoveryPassphrase { get; init; }
}

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

public sealed class PaymentResponse
{
    [JsonPropertyName("number")]
    public required int Number { get; init; }

    [JsonPropertyName("status")]
    public required PaymentStatus Status { get; init; }

    [JsonPropertyName("amount")]
    public required long Amount { get; init; }

    [JsonPropertyName("maxFee")]
    public required long MaxFee { get; init; }

    [JsonPropertyName("serviceFee")]
    public required long ServiceFee { get; init; }

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

public sealed class ApiKeyResponse
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("hint")]
    public required string Hint { get; init; }

    [JsonPropertyName("createdAt")]
    public DateTimeOffset? CreatedAt { get; init; }

    [JsonPropertyName("lastUsedAt")]
    public DateTimeOffset? LastUsedAt { get; init; }
}

public sealed class RotateApiKeyResponse
{
    [JsonPropertyName("key")]
    public required string Key { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }
}

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

public sealed class TransferAddressResponse
{
    [JsonPropertyName("address")]
    public required string Address { get; init; }

    [JsonPropertyName("transferredTo")]
    public required string TransferredTo { get; init; }
}

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
