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

### Send sats

```csharp
var payment = await w.Payments.CreateAsync(new CreatePaymentRequest
{
    Target = "alice@ln.bot",
    Amount = 500,
});
```

### Check balance

```csharp
var info = await w.GetAsync();
Console.WriteLine($"{info.Available} sats available");
```

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
w.Payments     // Send, list, get, watch, resolve payments
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

// Pay the challenge (client side)
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
