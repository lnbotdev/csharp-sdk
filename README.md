# ln.bot-csharp

[![NuGet](https://img.shields.io/nuget/v/LnBot)](https://www.nuget.org/packages/LnBot)
[![License: MIT](https://img.shields.io/badge/license-MIT-green)](./LICENSE)

**The official .NET SDK for [ln.bot](https://ln.bot)** — Bitcoin for AI Agents.

Give your AI agents, apps, and services access to Bitcoin over the Lightning Network. Create wallets, send and receive sats, and get real-time payment notifications.

```csharp
using LnBot;
using LnBot.Models;

using var client = new LnBotClient("uk_...");
var w = client.Wallet("wal_...");

var invoice = await w.Invoices.CreateAsync(new CreateInvoiceRequest
{
    Amount = 1000,
    Memo = "Coffee",
});
```

> ln.bot also ships a **[TypeScript SDK](https://www.npmjs.com/package/@lnbot/sdk)**, **[Python SDK](https://pypi.org/project/lnbot/)**, **[Go SDK](https://pkg.go.dev/github.com/lnbotdev/go-sdk)**, **[Rust SDK](https://crates.io/crates/lnbot)**, **[CLI](https://ln.bot/docs)**, and **[MCP server](https://ln.bot/docs)**.

---

## Install

```bash
dotnet add package LnBot
```

---

## Quick start

### Register an account

```csharp
using LnBot;

using var client = new LnBotClient();
var account = await client.RegisterAsync();
Console.WriteLine(account.PrimaryKey);
Console.WriteLine(account.RecoveryPassphrase);
```

### Create a wallet

```csharp
using var client = new LnBotClient(account.PrimaryKey);
var wallet = await client.Wallets.CreateAsync();
Console.WriteLine(wallet.WalletId);
```

### Receive sats

```csharp
var w = client.Wallet(wallet.WalletId);

var invoice = await w.Invoices.CreateAsync(new CreateInvoiceRequest
{
    Amount = 1000,
    Memo = "Payment for task #42",
});
Console.WriteLine(invoice.Bolt11);
```

### Wait for payment (SSE)

```csharp
await foreach (var evt in w.Invoices.WatchAsync(invoice.Number))
{
    if (evt.Event == "settled")
    {
        Console.WriteLine("Paid!");
        break;
    }
}
```

If the wait runs out first (`timeout` seconds, default 60, max 300), the stream yields a `timeout` event whose `evt.Data` is the invoice's current state; read it again or watch again.

Event and webhook payloads use snake_case on the wire (`actual_fee`, `created_at`); the SDK converts events to the camelCase fields of REST responses, while a webhook handler receives the snake_case names.

An `expired` invoice is not always final: one paid just before it expired can still turn `settled` afterwards. Before giving up, check again with `w.Invoices.GetAsync(invoice.Number)`, or keep listening for `invoice.settled` on `w.Events.StreamAsync()` or a webhook.

### Send sats

```csharp
var payment = await w.Payments.CreateAsync(new CreatePaymentRequest
{
    Target = "alice@ln.bot",
    Amount = 500,
});
```

Leave `MaxFee` unset so the server default applies (see [Payment fees](#payment-fees)). A payment that can't be sent fails quickly, and its funds are released at once: within about half a minute if the Lightning service refuses it, or after 10 minutes of automatic retries for a temporary problem (`PaymentStage.Retrying`, until `RetryUntil`). If its invoice expires first, it fails with `Invoice expired before it could be paid.` A payment stays `processing` longer only when its funds are already on the Lightning network: it settles or fails as soon as the network does, usually within seconds, and is never refunded while it could still be paid. Show `FailureReason` as is; when it ends in "Please try again.", sending the payment again later can work. Use `w.Payments.WatchAsync(payment.Number)` or `GetAsync` to follow it; `Stage` and `StatusDetail` say where it is (see [Payment stages](#payment-stages)).

### Check balance

```csharp
var info = await w.GetAsync();
Console.WriteLine($"{info.Available} sats available");
```

---

## Payment fees

A payment to an external wallet costs `Amount + ActualFee + ServiceFee`:

- `ActualFee` is the network fee actually charged: 0.25% plus Lightning routing.
- `ServiceFee` is 0.25%, minimum 1 sat.
- Transfers to another ln.bot wallet are free.

`MaxFee` caps the network fee. Leave it unset: the default is 1% of the amount, minimum 20 sats, and only the fee estimate plus a small buffer is held while the payment is in flight. A small hard-coded value rejects most larger payments. If the estimate is above the default limit, `CreateAsync` throws `BadRequestException` and you can set `MaxFee` to allow a higher fee.

### Show fees before paying

`ResolveAsync` prices a payment without sending anything. Pass the amount for a Lightning address or LNURL; a BOLT11 invoice is priced at its own amount.

```csharp
var quote = await w.Payments.ResolveAsync("alice@ln.bot", 5000);

Console.WriteLine($"Network fee: ~{quote.NetworkFeeEstimate} sats");
Console.WriteLine($"Service fee: {quote.ServiceFee} sats");
Console.WriteLine($"Total: {quote.Total} sats");

if (quote.FeeWarning is not null)
{
    // Sending without MaxFee would be rejected: the fee is unusually high
    Console.WriteLine(quote.FeeWarning);
}
```

Pricing a Lightning address or LNURL fetches an invoice from the recipient, so it is slower. Call it once the amount is settled, not on every keystroke.

### Send everything

Resolve a Lightning address or LNURL **without** an amount to get `MaxSendable`, the most this wallet can send there now after fees. Then send exactly that, without `MaxFee`:

```csharp
var target = "alice@ln.bot";
var info = await w.Payments.ResolveAsync(target);

if (info.MaxSendable is > 0)
{
    await w.Payments.CreateAsync(new CreatePaymentRequest
    {
        Target = target,
        Amount = info.MaxSendable,
    });
}
```

`MaxSendable` is 0 when the wallet can't cover the recipient's minimum.

### Resolve errors

Resolve throws `BadRequestException` when the target can't be priced. When the amount is outside the recipient's range, `ex.Body` is `{ "error", "min", "max" }`:

```csharp
using System.Text.Json;
using LnBot.Exceptions;

try
{
    var quote = await w.Payments.ResolveAsync("alice@ln.bot", 5000);
}
catch (BadRequestException ex)
{
    Console.WriteLine(ex.Message);

    using var body = JsonDocument.Parse(ex.Body);
    if (body.RootElement.TryGetProperty("min", out var min) && body.RootElement.TryGetProperty("max", out var max))
        Console.WriteLine($"Amount must be between {min} and {max} sats");
}
```

An unknown ln.bot address fails with `Recipient not found.` and your own address with `Cannot transfer to yourself.`

---

## Payment stages

Every payment returned by create, get and list carries `Stage`, where it is more precisely than `Status`, and `StatusDetail`, a sentence to show your user as is:

```csharp
var p = await w.Payments.GetAsync(payment.Number);

switch (p.Stage)
{
    case PaymentStage.Retrying:
        Console.WriteLine($"{p.StatusDetail} Retried until {p.RetryUntil:t}.");
        break;
    case PaymentStage.OnHold:
        Console.WriteLine(p.StatusDetail);
        break;
    default:
        // Any other stage, including one added after this SDK version: go by Status.
        var inProgress = p.Status is PaymentStatus.Pending or PaymentStatus.Processing;
        Console.WriteLine(inProgress ? "In progress" : p.Status.ToString());
        break;
}
```

| `Stage` | Constant | With `Status` | `StatusDetail` |
|---------|----------|---------------|----------------|
| `queued` | `PaymentStage.Queued` | `Pending` | Waiting to be sent. |
| `retrying` | `PaymentStage.Retrying` | `Pending` | Not sent yet; retrying for a few minutes. If it still can't be sent, it fails and the funds are released. |
| `sending` | `PaymentStage.Sending` | `Pending` or `Processing` | Being sent. Between ln.bot wallets: Being transferred to the recipient's wallet. |
| `in_flight` | `PaymentStage.InFlight` | `Processing` | Sent; the Lightning network is completing it, usually within seconds. |
| `confirming` | `PaymentStage.Confirming` | `Processing` | Checking with the Lightning network whether it went out. The funds stay on hold until then. |
| `on_hold` | `PaymentStage.OnHold` | `Processing` | Interrupted by an earlier version of the service and on hold until it is reviewed. The funds are held, not lost. |
| `settled` | `PaymentStage.Settled` | `Settled` | `null` |
| `failed` | `PaymentStage.Failed` | `Failed` | The `FailureReason` |

- `Stage` is a string, not an enum, because more stages may be added. For one you don't know, fall back to `Status`.
- A `retrying` payment carries `RetryUntil`: if it still can't be sent by then, it fails within about a minute and the funds are released.
- A payment can go from `Processing` back to `Pending` when it is retried, so treat both as in progress.
- From an API version that doesn't send them, `Stage`, `StatusDetail` and `RetryUntil` are null.
- `w.Payments.WatchAsync` events carry the same fields. Webhooks and `w.Events.StreamAsync()` don't.

---

## Service status

Two calls report whether ln.bot is working, measured about once a minute. They are public: they need no API key and no wallet, and a key the client has is sent and does no harm.

```csharp
using LnBot;
using LnBot.Models;

using var client = new LnBotClient();

var status = await client.GetStatusAsync();
Console.WriteLine(status.Status); // operational, degraded, outage or unknown

if (status.Notice is not null)
    Console.WriteLine($"{status.Notice.Level}: {status.Notice.Message}");

var sending = status.Components.Sending;
Console.WriteLine($"{sending.InProgress} payments being sent, median {sending.MedianSeconds}s");
foreach (var (stage, count) in sending.ByStage)
    Console.WriteLine($"  {stage}: {count}");
```

`Components` has `Sending` (payments out), `Receiving` (payments in), `Node` (the Lightning node) and `Webhooks` (deliveries), each with its own `Status`. Over time:

```csharp
var history = await client.GetStatusHistoryAsync(StatusRange.Last7Days);

Console.WriteLine($"Uptime: {history.Summary.Uptime.Overall.Operational}%");

foreach (var bucket in history.Buckets)
{
    if (bucket.Samples == 0) continue; // not measured: no data, not down
    var uptime = 100.0 * (bucket.Samples - bucket.Overall.Degraded - bucket.Overall.Outage) / bucket.Samples;
    Console.WriteLine($"{bucket.At:u} {uptime:F1}%, {bucket.Sending.Sent} sent");
}
```

- The range is `"24h"` (the default, 5-minute buckets), `"7d"` (hourly), `"30d"` (6-hourly) or `"90d"` (daily), also available as `StatusRange` constants. The API answers any other value with `BadRequestException`.
- Buckets are aligned to UTC. Don't assume how many there are: the last one is still filling.
- A bucket with `Samples` 0 wasn't measured, and its percentiles and gauges are null. Each part's `Degraded` and `Outage` count the bucket's measurements that found it in that state.
- `Summary` covers the whole range: uptime in percent of measured time (null when nothing was measured), totals, and the `SettleTimes` histogram.
- States and levels are strings, with the known values in `ServiceState` and `NoticeLevel`. More may be added, so don't fail on one you don't know.
- Both calls share the API's limit for public routes, 10 requests a minute per IP. Over it they throw `TooManyRequestsException` (status 429). The status changes once a minute, so there is no point polling it more often.

---

## Wallet-scoped API

All wallet operations go through a `WalletScope` obtained via `client.Wallet(walletId)`:

```csharp
var w = client.Wallet("wal_abc123");

// Wallet info
var info = await w.GetAsync();
await w.UpdateAsync(new UpdateWalletRequest { Name = "production" });

// Sub-resources
w.Key          // Wallet key management (wk_ keys)
w.Invoices     // Create, list, get, watch invoices
w.Payments     // Send, list, get, watch payments; resolve and price targets
w.Addresses    // Create, list, delete, transfer Lightning addresses
w.Transactions // List transaction history
w.Webhooks     // Create, list, delete webhook endpoints
w.Events       // Real-time SSE event stream
w.L402         // L402 paywall authentication
```

Account-level operations stay on the client:

```csharp
await client.RegisterAsync();          // Register new account
await client.MeAsync();               // Get authenticated identity
await client.GetStatusAsync();         // Service status now (public)
await client.GetStatusHistoryAsync();  // Service status over time (public)
await client.Wallets.CreateAsync();    // Create wallet
await client.Wallets.ListAsync();      // List wallets
await client.Keys.RotateAsync(0);     // Rotate account key
```

---

## Error handling

```csharp
using LnBot.Exceptions;

try
{
    var info = await w.GetAsync();
}
catch (NotFoundException ex)
{
    Console.WriteLine($"Not found: {ex.Message}");
}
catch (BadRequestException ex)
{
    Console.WriteLine($"Bad request: {ex.Message}");
}
catch (ConflictException ex)
{
    Console.WriteLine($"Conflict: {ex.Message}");
}
catch (TooManyRequestsException ex)
{
    Console.WriteLine($"Rate limited: {ex.Message}");
}
catch (LnBotException ex)
{
    Console.WriteLine($"API error {ex.StatusCode}: {ex.Message}");
}
```

## Configuration

```csharp
using var client = new LnBotClient("uk_...", new LnBotClientOptions
{
    BaseUrl = "https://api.ln.bot",
    Timeout = TimeSpan.FromSeconds(30),
});
```

Or bring your own `HttpClient`:

```csharp
var httpClient = new HttpClient();
using var client = new LnBotClient("uk_...", new LnBotClientOptions
{
    HttpClient = httpClient,
});
```

---

## L402 paywalls

```csharp
var w = client.Wallet("wal_...");

// Create a challenge (server side)
var challenge = await w.L402.CreateChallengeAsync(new CreateL402ChallengeRequest
{
    Amount = 100,
    Description = "API access",
    ExpirySeconds = 3600,
});

// Pay the challenge (client side). MaxFee works as for payments: leave it unset.
// result.Fee is the network fee actually charged.
var result = await w.L402.PayAsync(new PayL402Request
{
    WwwAuthenticate = challenge.WwwAuthenticate,
});

// Verify a token (server side, stateless)
var v = await w.L402.VerifyAsync(new VerifyL402Request
{
    Authorization = result.Authorization!,
});
```

---

## Features

- **Zero dependencies** — `System.Net.Http` + `System.Text.Json` only
- **Wallet-scoped API** — `client.Wallet(id)` returns a typed scope with all sub-resources
- **Async-first** — every method returns `Task<T>` with `CancellationToken` support
- **Typed exceptions** — `BadRequestException`, `NotFoundException`, `ConflictException`, `UnauthorizedException`, `ForbiddenException`, `TooManyRequestsException`
- **SSE support** — `WatchAsync` returns `IAsyncEnumerable<T>` for real-time events
- **Nullable reference types** — fully annotated

## Requirements

- .NET 8.0+
- Get your API key at [ln.bot](https://ln.bot)

## Links

- [ln.bot](https://ln.bot) — website
- [Documentation](https://ln.bot/docs)
- [GitHub](https://github.com/lnbotdev)
- [NuGet](https://www.nuget.org/packages/LnBot)

## Other SDKs

- [TypeScript SDK](https://github.com/lnbotdev/typescript-sdk) · [npm](https://www.npmjs.com/package/@lnbot/sdk)
- [Python SDK](https://github.com/lnbotdev/python-sdk) · [pypi](https://pypi.org/project/lnbot/)
- [Go SDK](https://github.com/lnbotdev/go-sdk) · [pkg.go.dev](https://pkg.go.dev/github.com/lnbotdev/go-sdk)
- [Rust SDK](https://github.com/lnbotdev/rust-sdk) · [crates.io](https://crates.io/crates/lnbot) · [docs.rs](https://docs.rs/lnbot)

## License

MIT
