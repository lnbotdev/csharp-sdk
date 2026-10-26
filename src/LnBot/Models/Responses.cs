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
// Service status
//
// Every state and level is an open string (see ServiceState and NoticeLevel), and no property is
// required, so a response with new values or without some fields still deserializes.
// ---------------------------------------------------------------------------

/// <summary>The service's status right now, from <see cref="LnBotClient.GetStatusAsync"/>. Measured about once a minute.</summary>
public sealed class ServiceStatusResponse
{
    /// <summary>
    /// Overall status: one of the <see cref="ServiceState"/> values. <c>unknown</c> when it hasn't been measured for
    /// 5 minutes; the numbers are then as old as <see cref="UpdatedAt"/>.
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; init; } = "";

    /// <summary>When it was measured. Null if never.</summary>
    [JsonPropertyName("updatedAt")]
    public DateTimeOffset? UpdatedAt { get; init; }

    /// <summary>A message from the operators, or null.</summary>
    [JsonPropertyName("notice")]
    public ServiceNotice? Notice { get; init; }

    [JsonPropertyName("components")]
    public ServiceComponents Components { get; init; } = new();
}

/// <summary>A message from the operators.</summary>
public sealed class ServiceNotice
{
    /// <summary>
    /// One of the <see cref="NoticeLevel"/> values: <c>info</c> (no effect on the status), <c>warning</c>
    /// (overall at least <c>degraded</c>) or <c>outage</c> (overall <c>outage</c>).
    /// </summary>
    [JsonPropertyName("level")]
    public string Level { get; init; } = "";

    /// <summary>Up to 500 characters, to show as is.</summary>
    [JsonPropertyName("message")]
    public string Message { get; init; } = "";

    /// <summary>When it was posted.</summary>
    [JsonPropertyName("since")]
    public DateTimeOffset Since { get; init; }

    /// <summary>When it was taken down. Always null in <see cref="ServiceStatusResponse.Notice"/>, and null in the history while it is still up.</summary>
    [JsonPropertyName("until")]
    public DateTimeOffset? Until { get; init; }
}

/// <summary>The status of each part of the service.</summary>
public sealed class ServiceComponents
{
    /// <summary>Lightning payments out.</summary>
    [JsonPropertyName("sending")]
    public SendingComponent Sending { get; init; } = new();

    /// <summary>Payments in.</summary>
    [JsonPropertyName("receiving")]
    public ReceivingComponent Receiving { get; init; } = new();

    /// <summary>The Lightning node.</summary>
    [JsonPropertyName("node")]
    public NodeComponent Node { get; init; } = new();

    /// <summary>Webhook delivery.</summary>
    [JsonPropertyName("webhooks")]
    public WebhooksComponent Webhooks { get; init; } = new();
}

/// <summary>Lightning payments out.</summary>
public sealed class SendingComponent
{
    /// <summary>One of the <see cref="ServiceState"/> values.</summary>
    [JsonPropertyName("status")]
    public string Status { get; init; } = "";

    /// <summary>Payments being sent now.</summary>
    [JsonPropertyName("inProgress")]
    public int InProgress { get; init; }

    /// <summary>
    /// The payments in progress by stage (see <see cref="PaymentStage"/>; more stages may appear).
    /// Stages with none are left out, so it can be empty.
    /// </summary>
    [JsonPropertyName("byStage")]
    public Dictionary<string, int> ByStage { get; init; } = new();

    /// <summary>How long the oldest payment in progress has waited, in seconds. Null when none is.</summary>
    [JsonPropertyName("longestWaitSeconds")]
    public double? LongestWaitSeconds { get; init; }

    /// <summary>Payments sent in the last hour.</summary>
    [JsonPropertyName("sentLastHour")]
    public int SentLastHour { get; init; }

    /// <summary>Payments failed in the last hour, for any reason.</summary>
    [JsonPropertyName("failedLastHour")]
    public int FailedLastHour { get; init; }

    /// <summary>Of <see cref="FailedLastHour"/>, the ones the service caused. The rest were the invoice's or the route's fault.</summary>
    [JsonPropertyName("serviceFailuresLastHour")]
    public int ServiceFailuresLastHour { get; init; }

    /// <summary>Median request-to-settlement time in seconds over the last hour. Null when nothing was sent.</summary>
    [JsonPropertyName("medianSeconds")]
    public double? MedianSeconds { get; init; }

    /// <summary>95th percentile request-to-settlement time in seconds over the last hour. Null when nothing was sent.</summary>
    [JsonPropertyName("p95Seconds")]
    public double? P95Seconds { get; init; }
}

/// <summary>Payments in.</summary>
public sealed class ReceivingComponent
{
    /// <summary>One of the <see cref="ServiceState"/> values.</summary>
    [JsonPropertyName("status")]
    public string Status { get; init; } = "";

    /// <summary>Incoming payments credited in the last hour.</summary>
    [JsonPropertyName("creditedLastHour")]
    public int CreditedLastHour { get; init; }

    /// <summary>Incoming payments waiting to be credited now. Null when not measured.</summary>
    [JsonPropertyName("creditBacklog")]
    public long? CreditBacklog { get; init; }
}

/// <summary>The Lightning node.</summary>
public sealed class NodeComponent
{
    /// <summary>One of the <see cref="ServiceState"/> values.</summary>
    [JsonPropertyName("status")]
    public string Status { get; init; } = "";

    /// <summary>How long the node took to answer, in milliseconds. Null when it didn't answer.</summary>
    [JsonPropertyName("latencyMs")]
    public long? LatencyMs { get; init; }
}

/// <summary>Webhook delivery.</summary>
public sealed class WebhooksComponent
{
    /// <summary>One of the <see cref="ServiceState"/> values.</summary>
    [JsonPropertyName("status")]
    public string Status { get; init; } = "";

    /// <summary>Deliveries waiting to go out. Null when not measured.</summary>
    [JsonPropertyName("backlog")]
    public long? Backlog { get; init; }
}

/// <summary>
/// The service over time, from <see cref="LnBotClient.GetStatusHistoryAsync"/>: a summary of the whole range and
/// one bucket per <see cref="BucketSeconds"/>, aligned to UTC. Don't assume the number of buckets: the last one is
/// the current bucket, still filling.
/// </summary>
public sealed class ServiceStatusHistoryResponse
{
    /// <summary>The range: one of the <see cref="StatusRange"/> values.</summary>
    [JsonPropertyName("range")]
    public string Range { get; init; } = "";

    /// <summary>The length of each bucket in seconds: 300, 3600, 21600 or 86400.</summary>
    [JsonPropertyName("bucketSeconds")]
    public int BucketSeconds { get; init; }

    [JsonPropertyName("from")]
    public DateTimeOffset From { get; init; }

    [JsonPropertyName("to")]
    public DateTimeOffset To { get; init; }

    /// <summary>The whole range.</summary>
    [JsonPropertyName("summary")]
    public StatusHistorySummary Summary { get; init; } = new();

    /// <summary>The buckets, oldest first.</summary>
    [JsonPropertyName("buckets")]
    public List<StatusBucket> Buckets { get; init; } = new();

    /// <summary>The operators' notices that were up during the range, oldest first.</summary>
    [JsonPropertyName("notices")]
    public List<ServiceNotice> Notices { get; init; } = new();
}

/// <summary>The whole range of a <see cref="ServiceStatusHistoryResponse"/>.</summary>
public sealed class StatusHistorySummary
{
    /// <summary>Measurements in the range, about one a minute.</summary>
    [JsonPropertyName("samples")]
    public int Samples { get; init; }

    /// <summary>Uptime per part and overall, in percent of measured time.</summary>
    [JsonPropertyName("uptime")]
    public StatusUptime Uptime { get; init; } = new();

    /// <summary>Payments sent.</summary>
    [JsonPropertyName("sent")]
    public int Sent { get; init; }

    /// <summary>Payments failed, for any reason.</summary>
    [JsonPropertyName("failed")]
    public int Failed { get; init; }

    /// <summary>Of <see cref="Failed"/>, the ones the service caused.</summary>
    [JsonPropertyName("serviceFailures")]
    public int ServiceFailures { get; init; }

    /// <summary>Incoming payments credited.</summary>
    [JsonPropertyName("credited")]
    public int Credited { get; init; }

    /// <summary>Median request-to-settlement time in seconds. Null when nothing was sent.</summary>
    [JsonPropertyName("medianSeconds")]
    public double? MedianSeconds { get; init; }

    /// <summary>95th percentile request-to-settlement time in seconds. Null when nothing was sent.</summary>
    [JsonPropertyName("p95Seconds")]
    public double? P95Seconds { get; init; }

    /// <summary>
    /// A histogram of settlement times. Its upper bounds are 1, 2, 3, 5, 10, 20, 30, 60, 120, 300 and 600 seconds;
    /// the last entry, with a null <see cref="SettleTimeBucket.UpToSeconds"/>, holds anything slower.
    /// </summary>
    [JsonPropertyName("settleTimes")]
    public List<SettleTimeBucket> SettleTimes { get; init; } = new();

    /// <summary>The node's average response time in milliseconds. Null when not measured.</summary>
    [JsonPropertyName("avgLatencyMs")]
    public double? AvgLatencyMs { get; init; }

    /// <summary>The node's slowest response time in milliseconds. Null when not measured.</summary>
    [JsonPropertyName("maxLatencyMs")]
    public long? MaxLatencyMs { get; init; }
}

/// <summary>Uptime per part and overall, in percent of measured time.</summary>
public sealed class StatusUptime
{
    [JsonPropertyName("overall")]
    public UptimePercentages Overall { get; init; } = new();

    [JsonPropertyName("sending")]
    public UptimePercentages Sending { get; init; } = new();

    [JsonPropertyName("receiving")]
    public UptimePercentages Receiving { get; init; } = new();

    [JsonPropertyName("node")]
    public UptimePercentages Node { get; init; } = new();

    [JsonPropertyName("webhooks")]
    public UptimePercentages Webhooks { get; init; } = new();
}

/// <summary>Percent of measured time in each state. Null when nothing was measured.</summary>
public sealed class UptimePercentages
{
    [JsonPropertyName("operational")]
    public double? Operational { get; init; }

    [JsonPropertyName("degraded")]
    public double? Degraded { get; init; }

    [JsonPropertyName("outage")]
    public double? Outage { get; init; }
}

/// <summary>One entry of the settlement time histogram.</summary>
public sealed class SettleTimeBucket
{
    /// <summary>The upper bound in seconds. Null for the last entry, which holds anything slower than the bound before it.</summary>
    [JsonPropertyName("upToSeconds")]
    public double? UpToSeconds { get; init; }

    /// <summary>Payments that settled within this bound and above the one before it.</summary>
    [JsonPropertyName("count")]
    public int Count { get; init; }
}

/// <summary>
/// One time bucket of a <see cref="ServiceStatusHistoryResponse"/>. A bucket with <see cref="Samples"/> 0 was not
/// measured, and its percentiles and gauges are null; that is different from a measured zero.
/// </summary>
public sealed class StatusBucket
{
    /// <summary>When the bucket starts.</summary>
    [JsonPropertyName("at")]
    public DateTimeOffset At { get; init; }

    /// <summary>Measurements in the bucket, about one a minute. 0 means not measured.</summary>
    [JsonPropertyName("samples")]
    public int Samples { get; init; }

    [JsonPropertyName("overall")]
    public StatusBucketPart Overall { get; init; } = new();

    [JsonPropertyName("sending")]
    public StatusBucketSending Sending { get; init; } = new();

    [JsonPropertyName("receiving")]
    public StatusBucketReceiving Receiving { get; init; } = new();

    [JsonPropertyName("node")]
    public StatusBucketNode Node { get; init; } = new();

    [JsonPropertyName("webhooks")]
    public StatusBucketWebhooks Webhooks { get; init; } = new();
}

/// <summary>
/// How many of a bucket's measurements found a part, or the service overall, degraded or in an outage.
/// The bucket's uptime is <c>(Samples - Degraded - Outage) / Samples</c>.
/// </summary>
public class StatusBucketPart
{
    [JsonPropertyName("degraded")]
    public int Degraded { get; init; }

    [JsonPropertyName("outage")]
    public int Outage { get; init; }
}

/// <summary>Lightning payments out, in one bucket.</summary>
public sealed class StatusBucketSending : StatusBucketPart
{
    /// <summary>Payments sent.</summary>
    [JsonPropertyName("sent")]
    public int Sent { get; init; }

    /// <summary>Payments failed, for any reason.</summary>
    [JsonPropertyName("failed")]
    public int Failed { get; init; }

    /// <summary>Of <see cref="Failed"/>, the ones the service caused.</summary>
    [JsonPropertyName("serviceFailures")]
    public int ServiceFailures { get; init; }

    /// <summary>Median request-to-settlement time in seconds. Null when nothing was sent.</summary>
    [JsonPropertyName("medianSeconds")]
    public double? MedianSeconds { get; init; }

    /// <summary>95th percentile request-to-settlement time in seconds. Null when nothing was sent.</summary>
    [JsonPropertyName("p95Seconds")]
    public double? P95Seconds { get; init; }

    /// <summary>The most payments in progress at once. Null when not measured.</summary>
    [JsonPropertyName("maxInProgress")]
    public int? MaxInProgress { get; init; }

    /// <summary>The longest wait of a payment in progress, in seconds. Null when none was waiting or not measured.</summary>
    [JsonPropertyName("maxWaitSeconds")]
    public double? MaxWaitSeconds { get; init; }
}

/// <summary>Payments in, in one bucket.</summary>
public sealed class StatusBucketReceiving : StatusBucketPart
{
    /// <summary>Incoming payments credited.</summary>
    [JsonPropertyName("credited")]
    public int Credited { get; init; }

    /// <summary>The most incoming payments waiting to be credited at once. Null when not measured.</summary>
    [JsonPropertyName("maxBacklog")]
    public long? MaxBacklog { get; init; }
}

/// <summary>The Lightning node, in one bucket.</summary>
public sealed class StatusBucketNode : StatusBucketPart
{
    /// <summary>The node's average response time in milliseconds. Null when not measured.</summary>
    [JsonPropertyName("avgLatencyMs")]
    public double? AvgLatencyMs { get; init; }

    /// <summary>The node's slowest response time in milliseconds. Null when not measured.</summary>
    [JsonPropertyName("maxLatencyMs")]
    public long? MaxLatencyMs { get; init; }
}

/// <summary>Webhook delivery, in one bucket.</summary>
public sealed class StatusBucketWebhooks : StatusBucketPart
{
    /// <summary>The most deliveries waiting to go out at once. Null when not measured.</summary>
    [JsonPropertyName("maxBacklog")]
    public long? MaxBacklog { get; init; }
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
    /// Network fee actually charged in sats: the Lightning routing (the 0.25% service fee is separate). Null until settled.
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

    /// <summary>
    /// Where the payment is, more precisely than <see cref="Status"/>: one of the <see cref="PaymentStage"/> values.
    /// More stages may be added, so for a value you don't know, fall back to <see cref="Status"/>.
    /// Null from an API version that doesn't send it.
    /// </summary>
    [JsonPropertyName("stage")]
    public string? Stage { get; init; }

    /// <summary>
    /// A sentence to show the user as is. While in progress, what is happening; once failed, the
    /// <see cref="FailureReason"/>; once settled, null.
    /// </summary>
    [JsonPropertyName("statusDetail")]
    public string? StatusDetail { get; init; }

    /// <summary>
    /// Only with <see cref="Stage"/> <c>retrying</c>: until when the payment is retried. If it still can't be sent
    /// by then, it fails within about a minute and the funds are released. Null otherwise.
    /// </summary>
    [JsonPropertyName("retryUntil")]
    public DateTimeOffset? RetryUntil { get; init; }
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
    /// Estimated network fee in sats: the Lightning routing.
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

    /// <summary>Network fee actually charged in sats: the Lightning routing (the 0.25% service fee is separate). Null until settled.</summary>
    [JsonPropertyName("fee")]
    public long? Fee { get; init; }

    [JsonPropertyName("paymentNumber")]
    public required int PaymentNumber { get; init; }

    [JsonPropertyName("status")]
    public required string Status { get; init; }
}
