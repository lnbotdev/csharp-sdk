namespace LnBot.Models;

public enum InvoiceStatus
{
    Pending,
    Settled,
    Expired,
}

public enum PaymentStatus
{
    Pending,
    Processing,
    Settled,
    Failed,
}

public enum TransactionType
{
    Credit,
    Debit,
}

// ---------------------------------------------------------------------------
// Open value sets
//
// These fields are plain strings, not enums: the API may add values at any time, and the
// serializer's enum converter would throw on a value it doesn't know. Compare against these
// constants and handle anything else as unknown.
// ---------------------------------------------------------------------------

/// <summary>
/// Known values of <see cref="PaymentResponse.Stage"/>: where a payment is, more precisely than its
/// <see cref="PaymentStatus"/>. More stages may be added; for one you don't know, fall back to the payment's status.
/// </summary>
public static class PaymentStage
{
    /// <summary>Waiting to be sent. Status <c>pending</c>.</summary>
    public const string Queued = "queued";

    /// <summary>
    /// Not sent yet and retried until <see cref="PaymentResponse.RetryUntil"/>; if it still can't be sent by then,
    /// it fails and the funds are released. Status <c>pending</c>, possibly after <c>processing</c>.
    /// </summary>
    public const string Retrying = "retrying";

    /// <summary>Being sent, or for a transfer between ln.bot wallets, being transferred. Status <c>pending</c> or <c>processing</c>.</summary>
    public const string Sending = "sending";

    /// <summary>Sent; the Lightning network is completing it, usually within seconds. Status <c>processing</c>.</summary>
    public const string InFlight = "in_flight";

    /// <summary>Checking with the Lightning network whether it went out; the funds stay on hold until then. Status <c>processing</c>.</summary>
    public const string Confirming = "confirming";

    /// <summary>Interrupted by an earlier version of the service and on hold until it is reviewed; the funds are held, not lost. Status <c>processing</c>.</summary>
    public const string OnHold = "on_hold";

    /// <summary>Settled. Status <c>settled</c>.</summary>
    public const string Settled = "settled";

    /// <summary>Failed; <see cref="PaymentResponse.StatusDetail"/> is the failure reason. Status <c>failed</c>.</summary>
    public const string Failed = "failed";
}

/// <summary>
/// Known values of the service's overall and per-part status in <see cref="ServiceStatusResponse"/>.
/// More values may be added; treat one you don't know like <see cref="Unknown"/>.
/// </summary>
public static class ServiceState
{
    /// <summary>Working normally.</summary>
    public const string Operational = "operational";

    /// <summary>Working, but slower or less reliable than usual.</summary>
    public const string Degraded = "degraded";

    /// <summary>Not working.</summary>
    public const string Outage = "outage";

    /// <summary>Not measured for 5 minutes; the numbers are as old as <see cref="ServiceStatusResponse.UpdatedAt"/>.</summary>
    public const string Unknown = "unknown";
}

/// <summary>
/// Known values of <see cref="ServiceNotice.Level"/>. More values may be added.
/// </summary>
public static class NoticeLevel
{
    /// <summary>For information; it doesn't affect the status.</summary>
    public const string Info = "info";

    /// <summary>The overall status is at least <c>degraded</c>.</summary>
    public const string Warning = "warning";

    /// <summary>The overall status is <c>outage</c>.</summary>
    public const string Outage = "outage";
}

/// <summary>
/// Known values of the <c>range</c> argument of <see cref="LnBotClient.GetStatusHistoryAsync"/> and of
/// <see cref="ServiceStatusHistoryResponse.Range"/>. More values may be added.
/// </summary>
public static class StatusRange
{
    /// <summary>The last 24 hours, in 5-minute buckets. The default.</summary>
    public const string Last24Hours = "24h";

    /// <summary>The last 7 days, in 1-hour buckets.</summary>
    public const string Last7Days = "7d";

    /// <summary>The last 30 days, in 6-hour buckets.</summary>
    public const string Last30Days = "30d";

    /// <summary>The last 90 days, in 1-day buckets.</summary>
    public const string Last90Days = "90d";
}
