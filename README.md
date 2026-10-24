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

Leave `MaxFee` unset so the server default applies (see [Payment fees](#payment-fees)). A payment can stay `pending` for a while because it is retried automatically. Retries stop when the invoice expires (failure reason `Invoice expired before it could be paid.`) or after 24 hours at most. A failing payment can also stay `processing` a few seconds before it turns `failed`. Use `w.Payments.WatchAsync(payment.Number)` or `GetAsync` to follow it.

### Check balance

```csharp
var info = await w.GetAsync();
Console.WriteLine($"{info.Available} sats available");
```

---

## Payment fees

A payment to an external wallet costs `Amount + ActualFee + ServiceFee`:

- `ActualFee` is the network fee actually charged: Spark's 0.25% plus Lightning routing.
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
- **Typed exceptions** — `BadRequestException`, `NotFoundException`, `ConflictException`, `UnauthorizedException`, `ForbiddenException`
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
