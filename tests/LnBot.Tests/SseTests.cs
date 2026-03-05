using Xunit;

namespace LnBot.Tests;

public class InvoiceWatchTests
{
    [Fact]
    public async Task WatchAsync_YieldsInvoiceEvents()
    {
        var sse = "data: {\"event\":\"settled\",\"data\":{\"number\":1,\"status\":\"settled\",\"amount\":100,\"bolt11\":\"lnbc1...\"}}\n\n";
        var (client, _) = TestHelper.CreateSseClient(sse);

        var events = new List<Models.InvoiceEvent>();
        await foreach (var evt in client.Wallet("wal_1").Invoices.WatchAsync(1))
            events.Add(evt);

        Assert.Single(events);
        Assert.Equal("settled", events[0].Event);
        Assert.Equal(1, events[0].Data.Number);
    }

    [Fact]
    public async Task WatchAsync_YieldsMultipleEvents()
    {
        var sse =
            "data: {\"event\":\"pending\",\"data\":{\"number\":1,\"status\":\"pending\",\"amount\":50,\"bolt11\":\"lnbc1...\"}}\n\n" +
            "data: {\"event\":\"settled\",\"data\":{\"number\":1,\"status\":\"settled\",\"amount\":50,\"bolt11\":\"lnbc1...\"}}\n\n";
        var (client, _) = TestHelper.CreateSseClient(sse);

        var events = new List<Models.InvoiceEvent>();
        await foreach (var evt in client.Wallet("wal_1").Invoices.WatchAsync(1))
            events.Add(evt);

        Assert.Equal(2, events.Count);
        Assert.Equal("pending", events[0].Event);
        Assert.Equal("settled", events[1].Event);
    }

    [Fact]
    public async Task WatchAsync_SkipsNonDataLines()
    {
        var sse =
            ": keepalive\n\n" +
            "event: settled\n" +
            "data: {\"event\":\"settled\",\"data\":{\"number\":1,\"status\":\"settled\",\"amount\":100,\"bolt11\":\"lnbc1...\"}}\n\n";
        var (client, _) = TestHelper.CreateSseClient(sse);

        var events = new List<Models.InvoiceEvent>();
        await foreach (var evt in client.Wallet("wal_1").Invoices.WatchAsync(1))
            events.Add(evt);

        Assert.Single(events);
    }

    [Fact]
    public async Task WatchAsync_BuildsCorrectPath()
    {
        var (client, handler) = TestHelper.CreateSseClient("");

        await foreach (var _ in client.Wallet("wal_1").Invoices.WatchAsync(42, timeout: 120)) { }

        Assert.Contains("/v1/wallets/wal_1/invoices/42/events", handler.LastRequest!.RequestUri!.AbsolutePath);
        Assert.Contains("timeout=120", handler.LastRequest.RequestUri!.Query);
    }

    [Fact]
    public async Task WatchAsync_OmitsTimeoutWhenNull()
    {
        var (client, handler) = TestHelper.CreateSseClient("");

        await foreach (var _ in client.Wallet("wal_1").Invoices.WatchAsync(1)) { }

        Assert.DoesNotContain("timeout", handler.LastRequest!.RequestUri!.ToString());
    }

    [Fact]
    public async Task WatchAsync_SendsSseAcceptHeader()
    {
        var (client, handler) = TestHelper.CreateSseClient("");

        await foreach (var _ in client.Wallet("wal_1").Invoices.WatchAsync(1)) { }

        Assert.Contains("text/event-stream", handler.LastRequest!.Headers.Accept.ToString());
    }

    [Fact]
    public async Task WatchByHashAsync_EscapesHash()
    {
        var (client, handler) = TestHelper.CreateSseClient("");

        await foreach (var _ in client.Wallet("wal_1").Invoices.WatchByHashAsync("abc/123")) { }

        Assert.Contains("/v1/wallets/wal_1/invoices/abc%2F123/events", handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task WatchAsync_HandlesEmptyStream()
    {
        var (client, _) = TestHelper.CreateSseClient("");

        var events = new List<Models.InvoiceEvent>();
        await foreach (var evt in client.Wallet("wal_1").Invoices.WatchAsync(1))
            events.Add(evt);

        Assert.Empty(events);
    }
}

public class PaymentWatchTests
{
    [Fact]
    public async Task WatchAsync_YieldsPaymentEvents()
    {
        var sse = "data: {\"event\":\"settled\",\"data\":{\"number\":1,\"status\":\"settled\",\"amount\":50,\"maxFee\":10,\"serviceFee\":0,\"address\":\"user@ln.bot\"}}\n\n";
        var (client, _) = TestHelper.CreateSseClient(sse);

        var events = new List<Models.PaymentEvent>();
        await foreach (var evt in client.Wallet("wal_1").Payments.WatchAsync(1))
            events.Add(evt);

        Assert.Single(events);
        Assert.Equal("settled", events[0].Event);
        Assert.Equal(50, events[0].Data.Amount);
    }

    [Fact]
    public async Task WatchAsync_BuildsCorrectPath()
    {
        var (client, handler) = TestHelper.CreateSseClient("");

        await foreach (var _ in client.Wallet("wal_1").Payments.WatchAsync(7, timeout: 60)) { }

        Assert.Contains("/v1/wallets/wal_1/payments/7/events", handler.LastRequest!.RequestUri!.AbsolutePath);
        Assert.Contains("timeout=60", handler.LastRequest.RequestUri!.Query);
    }

    [Fact]
    public async Task WatchByHashAsync_BuildsCorrectPath()
    {
        var (client, handler) = TestHelper.CreateSseClient("");

        await foreach (var _ in client.Wallet("wal_1").Payments.WatchByHashAsync("hash123")) { }

        Assert.Contains("/v1/wallets/wal_1/payments/hash123/events", handler.LastRequest!.RequestUri!.AbsolutePath);
    }
}

public class EventStreamTests
{
    [Fact]
    public async Task StreamAsync_YieldsWalletEvents()
    {
        var sse = "data: {\"event\":\"invoice.settled\",\"createdAt\":\"2024-01-01T00:00:00Z\",\"data\":{\"number\":1,\"status\":\"settled\",\"amount\":100}}\n\n";
        var (client, _) = TestHelper.CreateSseClient(sse);

        var events = new List<Models.WalletEvent>();
        await foreach (var evt in client.Wallet("wal_1").Events.StreamAsync())
            events.Add(evt);

        Assert.Single(events);
        Assert.Equal("invoice.settled", events[0].Event);
    }

    [Fact]
    public async Task StreamAsync_BuildsCorrectPath()
    {
        var (client, handler) = TestHelper.CreateSseClient("");

        await foreach (var _ in client.Wallet("wal_1").Events.StreamAsync()) { }

        Assert.EndsWith("/v1/wallets/wal_1/events", handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task StreamAsync_SkipsNonDataLines()
    {
        var sse =
            ": keepalive\n" +
            "event: ignored\n" +
            "data: {\"event\":\"payment.settled\",\"createdAt\":\"2024-01-01T00:00:00Z\",\"data\":{\"number\":1,\"status\":\"settled\",\"amount\":50}}\n\n";
        var (client, _) = TestHelper.CreateSseClient(sse);

        var events = new List<Models.WalletEvent>();
        await foreach (var evt in client.Wallet("wal_1").Events.StreamAsync())
            events.Add(evt);

        Assert.Single(events);
        Assert.Equal("payment.settled", events[0].Event);
    }
}
