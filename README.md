# ln.bot-csharp

[![NuGet](https://img.shields.io/nuget/v/LnBot)](https://www.nuget.org/packages/LnBot)
[![License: MIT](https://img.shields.io/badge/license-MIT-green)](./LICENSE)

**The official .NET SDK for [ln.bot](https://ln.bot)** — Bitcoin for AI Agents.

Give your AI agents, apps, and services access to Bitcoin over the Lightning Network. Create wallets, send and receive sats, and get real-time payment notifications.

```csharp
using LnBot;
using LnBot.Models;

using var client = new LnBotClient("key_...");

var invoice = await client.Invoices.CreateAsync(new CreateInvoiceRequest
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

### Create a wallet

```csharp
using LnBot;
using LnBot.Models;

using var client = new LnBotClient();

var wallet = await client.Wallets.CreateAsync(new CreateWalletRequest
{
    Name = "my-agent",
});
Console.WriteLine(wallet.PrimaryKey);
```

### Receive sats

```csharp
using var client = new LnBotClient(wallet.PrimaryKey);

var invoice = await client.Invoices.CreateAsync(new CreateInvoiceRequest
{
    Amount = 1000,
    Memo = "Payment for task #42",
});
Console.WriteLine(invoice.Bolt11);
```

### Wait for payment (SSE)

```csharp
await foreach (var evt in client.Invoices.WatchAsync(invoice.Number))
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
var payment = await client.Payments.CreateAsync(new CreatePaymentRequest
{
    Target = "alice@ln.bot",
    Amount = 500,
});
```

### Check balance

```csharp
var current = await client.Wallets.CurrentAsync();
Console.WriteLine($"{current.Available} sats available");
```

---

## Error handling

```csharp
using LnBot.Exceptions;

try
{
    var wallet = await client.Wallets.CurrentAsync();
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
using var client = new LnBotClient("key_...", new LnBotClientOptions
{
    BaseUrl = "https://api.ln.bot",
    Timeout = TimeSpan.FromSeconds(30),
});
```

Or bring your own `HttpClient`:

```csharp
var httpClient = new HttpClient();
using var client = new LnBotClient("key_...", new LnBotClientOptions
{
    HttpClient = httpClient,
});
```

---

## Features

- **Zero dependencies** — `System.Net.Http` + `System.Text.Json` only
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
