using System.Text.Json;
using System.Text.Json.Serialization;
using LnBot.Models;
using Xunit;

namespace LnBot.Tests;

/// <summary>
/// Event payloads as the API sends them: snake_case keys, and for the single invoice and
/// payment watch streams the event name in an <c>event:</c> line with the object itself as data.
/// </summary>
internal static class SsePayloads
{
    public const string SettledPayment =
        "{\"number\":1,\"status\":\"settled\",\"amount\":20,\"max_fee\":5,\"service_fee\":1,\"actual_fee\":3,\"address\":\"bub@bub.gg\",\"reference\":null,\"preimage\":\"ab12...\",\"tx_number\":4,\"failure_reason\":null,\"created_at\":\"2026-10-24T14:50:01Z\",\"settled_at\":\"2026-10-24T14:50:14Z\"}";

    public const string SettledInvoice =
        "{\"number\":7,\"status\":\"settled\",\"amount\":1000,\"bolt11\":\"lnbc10u1p...\",\"reference\":null,\"memo\":null,\"preimage\":\"cd34...\",\"tx_number\":4,\"created_at\":\"2026-10-24T14:49:00Z\",\"settled_at\":\"2026-10-24T14:50:14Z\",\"expires_at\":\"2026-10-24T15:49:00Z\"}";

    public static readonly DateTimeOffset PaymentCreatedAt = new(2026, 10, 24, 14, 50, 1, TimeSpan.Zero);
    public static readonly DateTimeOffset SettledAt = new(2026, 10, 24, 14, 50, 14, TimeSpan.Zero);
    public static readonly DateTimeOffset InvoiceCreatedAt = new(2026, 10, 24, 14, 49, 0, TimeSpan.Zero);
    public static readonly DateTimeOffset InvoiceExpiresAt = new(2026, 10, 24, 15, 49, 0, TimeSpan.Zero);

    /// <summary>Options matching the SDK's, for decoding <see cref="WalletEvent.Data"/>.</summary>
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };
}

public class InvoiceWatchTests
{
    [Fact]
    public async Task WatchAsync_DecodesSnakeCaseSettledEvent()
    {
        var sse = $"event: settled\ndata: {SsePayloads.SettledInvoice}\n\n";
        var (client, _) = TestHelper.CreateSseClient(sse);

        var events = new List<InvoiceEvent>();
        await foreach (var evt in client.Wallet("wal_1").Invoices.WatchAsync(7))
            events.Add(evt);

        var e = Assert.Single(events);
        Assert.Equal("settled", e.Event);
        Assert.Equal(7, e.Data.Number);
        Assert.Equal(InvoiceStatus.Settled, e.Data.Status);
        Assert.Equal(1000, e.Data.Amount);
        Assert.Equal("lnbc10u1p...", e.Data.Bolt11);
        Assert.Null(e.Data.Reference);
        Assert.Null(e.Data.Memo);
        Assert.Equal("cd34...", e.Data.Preimage);
        Assert.Equal(4, e.Data.TxNumber);
        Assert.Equal(SsePayloads.InvoiceCreatedAt, e.Data.CreatedAt);
        Assert.Equal(SsePayloads.SettledAt, e.Data.SettledAt);
        Assert.Equal(SsePayloads.InvoiceExpiresAt, e.Data.ExpiresAt);
    }

    [Fact]
    public async Task WatchAsync_YieldsTimeoutEventWithCurrentState()
    {
        var sse =
            "event: timeout\n" +
            "data: {\"number\":7,\"status\":\"pending\",\"amount\":1000,\"bolt11\":\"lnbc10u1p...\",\"reference\":\"order-42\",\"memo\":\"coffee\",\"preimage\":null,\"tx_number\":null,\"created_at\":\"2026-10-24T14:49:00Z\",\"settled_at\":null,\"expires_at\":\"2026-10-24T15:49:00Z\"}\n\n";
        var (client, _) = TestHelper.CreateSseClient(sse);

        var events = new List<InvoiceEvent>();
        await foreach (var evt in client.Wallet("wal_1").Invoices.WatchAsync(7, timeout: 1))
            events.Add(evt);

        var e = Assert.Single(events);
        Assert.Equal("timeout", e.Event);
        Assert.Equal(InvoiceStatus.Pending, e.Data.Status);
        Assert.Equal("order-42", e.Data.Reference);
        Assert.Equal("coffee", e.Data.Memo);
        Assert.Null(e.Data.TxNumber);
        Assert.Null(e.Data.SettledAt);
        Assert.Equal(SsePayloads.InvoiceExpiresAt, e.Data.ExpiresAt);
    }

    [Fact]
    public async Task WatchAsync_YieldsMultipleEvents()
    {
        var sse =
            "event: expired\n" +
            "data: {\"number\":1,\"status\":\"expired\",\"amount\":50,\"bolt11\":\"lnbc1...\"}\n\n" +
            "event: settled\n" +
            "data: {\"number\":1,\"status\":\"settled\",\"amount\":50,\"bolt11\":\"lnbc1...\",\"tx_number\":9}\n\n";
        var (client, _) = TestHelper.CreateSseClient(sse);

        var events = new List<InvoiceEvent>();
        await foreach (var evt in client.Wallet("wal_1").Invoices.WatchAsync(1))
            events.Add(evt);

        Assert.Equal(2, events.Count);
        Assert.Equal("expired", events[0].Event);
        Assert.Equal(InvoiceStatus.Expired, events[0].Data.Status);
        Assert.Equal("settled", events[1].Event);
        Assert.Equal(9, events[1].Data.TxNumber);
    }

    [Fact]
    public async Task WatchAsync_SkipsKeepaliveComments()
    {
        var sse =
            ":keepalive\n\n" +
            ": keepalive\n\n" +
            $"event: settled\ndata: {SsePayloads.SettledInvoice}\n\n";
        var (client, _) = TestHelper.CreateSseClient(sse);

        var events = new List<InvoiceEvent>();
        await foreach (var evt in client.Wallet("wal_1").Invoices.WatchAsync(1))
            events.Add(evt);

        Assert.Equal("settled", Assert.Single(events).Event);
    }

    [Fact]
    public async Task WatchAsync_JoinsMultiLineData()
    {
        var sse =
            "event: settled\n" +
            "data: {\"number\":7,\"status\":\"settled\",\n" +
            "data:\"amount\":1000,\"bolt11\":\"lnbc10u1p...\",\"tx_number\":4}\n\n";
        var (client, _) = TestHelper.CreateSseClient(sse);

        var events = new List<InvoiceEvent>();
        await foreach (var evt in client.Wallet("wal_1").Invoices.WatchAsync(7))
            events.Add(evt);

        var e = Assert.Single(events);
        Assert.Equal(1000, e.Data.Amount);
        Assert.Equal(4, e.Data.TxNumber);
    }

    [Fact]
    public async Task WatchAsync_DecodesCamelCasePayload()
    {
        var sse =
            "event: settled\n" +
            "data: {\"number\":7,\"status\":\"settled\",\"amount\":1000,\"bolt11\":\"lnbc10u1p...\",\"txNumber\":4,\"createdAt\":\"2026-10-24T14:49:00Z\",\"settledAt\":\"2026-10-24T14:50:14Z\",\"expiresAt\":\"2026-10-24T15:49:00Z\"}\n\n";
        var (client, _) = TestHelper.CreateSseClient(sse);

        var events = new List<InvoiceEvent>();
        await foreach (var evt in client.Wallet("wal_1").Invoices.WatchAsync(7))
            events.Add(evt);

        var e = Assert.Single(events);
        Assert.Equal(4, e.Data.TxNumber);
        Assert.Equal(SsePayloads.SettledAt, e.Data.SettledAt);
        Assert.Equal(SsePayloads.InvoiceExpiresAt, e.Data.ExpiresAt);
    }

    [Fact]
    public async Task WatchAsync_AcceptsEventEnvelopeInData()
    {
        // The {"event":...,"data":{...}} shape earlier SDK versions expected.
        var sse = "data: {\"event\":\"settled\",\"data\":{\"number\":1,\"status\":\"settled\",\"amount\":100,\"bolt11\":\"lnbc1...\"}}\n\n";
        var (client, _) = TestHelper.CreateSseClient(sse);

        var events = new List<InvoiceEvent>();
        await foreach (var evt in client.Wallet("wal_1").Invoices.WatchAsync(1))
            events.Add(evt);

        var e = Assert.Single(events);
        Assert.Equal("settled", e.Event);
        Assert.Equal(1, e.Data.Number);
    }

    [Fact]
    public async Task WatchAsync_SkipsUndecodableEvent()
    {
        var sse = "event: settled\ndata: {\"number\":7}\n\n" + $"event: settled\ndata: {SsePayloads.SettledInvoice}\n\n";
        var (client, _) = TestHelper.CreateSseClient(sse);

        var events = new List<InvoiceEvent>();
        await foreach (var evt in client.Wallet("wal_1").Invoices.WatchAsync(7))
            events.Add(evt);

        Assert.Equal("settled", Assert.Single(events).Event);
    }

    [Fact]
    public async Task WatchByHashAsync_DecodesSnakeCaseSettledEvent()
    {
        var sse = $"event: settled\ndata: {SsePayloads.SettledInvoice}\n\n";
        var (client, _) = TestHelper.CreateSseClient(sse);

        var events = new List<InvoiceEvent>();
        await foreach (var evt in client.Wallet("wal_1").Invoices.WatchByHashAsync("abc123"))
            events.Add(evt);

        var e = Assert.Single(events);
        Assert.Equal(4, e.Data.TxNumber);
        Assert.Equal(SsePayloads.SettledAt, e.Data.SettledAt);
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

        var events = new List<InvoiceEvent>();
        await foreach (var evt in client.Wallet("wal_1").Invoices.WatchAsync(1))
            events.Add(evt);

        Assert.Empty(events);
    }
}

public class PaymentWatchTests
{
    [Fact]
    public async Task WatchAsync_DecodesSnakeCaseSettledEvent()
    {
        var sse = $"event: settled\ndata: {SsePayloads.SettledPayment}\n\n";
        var (client, _) = TestHelper.CreateSseClient(sse);

        var events = new List<PaymentEvent>();
        await foreach (var evt in client.Wallet("wal_1").Payments.WatchAsync(1))
            events.Add(evt);

        var e = Assert.Single(events);
        Assert.Equal("settled", e.Event);
        Assert.Equal(1, e.Data.Number);
        Assert.Equal(PaymentStatus.Settled, e.Data.Status);
        Assert.Equal(20, e.Data.Amount);
        Assert.Equal(5, e.Data.MaxFee);
        Assert.Equal(1, e.Data.ServiceFee);
        Assert.Equal(3, e.Data.ActualFee);
        Assert.Equal("bub@bub.gg", e.Data.Address);
        Assert.Null(e.Data.Reference);
        Assert.Equal("ab12...", e.Data.Preimage);
        Assert.Equal(4, e.Data.TxNumber);
        Assert.Null(e.Data.FailureReason);
        Assert.Equal(SsePayloads.PaymentCreatedAt, e.Data.CreatedAt);
        Assert.Equal(SsePayloads.SettledAt, e.Data.SettledAt);
    }

    [Fact]
    public async Task WatchAsync_DecodesFailureReason()
    {
        var sse =
            "event: failed\n" +
            "data: {\"number\":2,\"status\":\"failed\",\"amount\":20,\"max_fee\":5,\"service_fee\":1,\"actual_fee\":null,\"address\":\"bub@bub.gg\",\"reference\":null,\"preimage\":null,\"tx_number\":null,\"failure_reason\":\"Invoice expired before it could be paid.\",\"created_at\":\"2026-10-24T14:50:01Z\",\"settled_at\":null}\n\n";
        var (client, _) = TestHelper.CreateSseClient(sse);

        var events = new List<PaymentEvent>();
        await foreach (var evt in client.Wallet("wal_1").Payments.WatchAsync(2))
            events.Add(evt);

        var e = Assert.Single(events);
        Assert.Equal("failed", e.Event);
        Assert.Equal(PaymentStatus.Failed, e.Data.Status);
        Assert.Equal("Invoice expired before it could be paid.", e.Data.FailureReason);
        Assert.Equal(5, e.Data.MaxFee);
        Assert.Equal(1, e.Data.ServiceFee);
        Assert.Null(e.Data.ActualFee);
        Assert.Null(e.Data.SettledAt);
    }

    [Fact]
    public async Task WatchAsync_YieldsTimeoutEventWithCurrentState()
    {
        var sse =
            ":keepalive\n\n" +
            "event: timeout\n" +
            "data: {\"number\":3,\"status\":\"processing\",\"amount\":20,\"max_fee\":5,\"service_fee\":1,\"actual_fee\":null,\"address\":\"bub@bub.gg\",\"reference\":\"job-9\",\"preimage\":null,\"tx_number\":null,\"failure_reason\":null,\"created_at\":\"2026-10-24T14:50:01Z\",\"settled_at\":null}\n\n";
        var (client, _) = TestHelper.CreateSseClient(sse);

        var events = new List<PaymentEvent>();
        await foreach (var evt in client.Wallet("wal_1").Payments.WatchAsync(3, timeout: 1))
            events.Add(evt);

        var e = Assert.Single(events);
        Assert.Equal("timeout", e.Event);
        Assert.Equal(PaymentStatus.Processing, e.Data.Status);
        Assert.Equal("job-9", e.Data.Reference);
        Assert.Equal(5, e.Data.MaxFee);
        Assert.Null(e.Data.ActualFee);
        Assert.Equal(SsePayloads.PaymentCreatedAt, e.Data.CreatedAt);
    }

    [Fact]
    public async Task WatchAsync_DecodesStageFieldsInTimeoutEvent()
    {
        var sse = $"event: timeout\ndata: {TestHelper.Fixture("payment-timeout-event.json")}\n\n";
        var (client, _) = TestHelper.CreateSseClient(sse);

        var events = new List<PaymentEvent>();
        await foreach (var evt in client.Wallet("wal_1").Payments.WatchAsync(4, timeout: 1))
            events.Add(evt);

        var e = Assert.Single(events);
        Assert.Equal("timeout", e.Event);
        Assert.Equal(PaymentStatus.Processing, e.Data.Status);
        Assert.Equal(PaymentStage.InFlight, e.Data.Stage);
        Assert.Equal("Sent; the Lightning network is completing it, usually within seconds.", e.Data.StatusDetail);
        Assert.Null(e.Data.RetryUntil);
        Assert.Equal(20, e.Data.MaxFee);
        Assert.Equal(3, e.Data.ServiceFee);
        Assert.Equal(new DateTimeOffset(2026, 10, 25, 14, 33, 53, TimeSpan.Zero), e.Data.CreatedAt);
    }

    [Fact]
    public async Task WatchAsync_DecodesSnakeCaseRetryUntil()
    {
        var sse =
            "event: timeout\n" +
            "data: {\"number\":4,\"status\":\"pending\",\"amount\":1000,\"max_fee\":20,\"service_fee\":3,\"actual_fee\":null,\"address\":\"lnbc10u1p...\",\"reference\":null,\"preimage\":null,\"tx_number\":null,\"failure_reason\":null,\"created_at\":\"2026-10-25T14:33:53+00:00\",\"settled_at\":null,\"stage\":\"retrying\",\"status_detail\":\"Not sent yet; retrying for a few minutes. If it still can't be sent, it fails and the funds are released.\",\"retry_until\":\"2026-10-25T14:43:53+00:00\"}\n\n";
        var (client, _) = TestHelper.CreateSseClient(sse);

        var events = new List<PaymentEvent>();
        await foreach (var evt in client.Wallet("wal_1").Payments.WatchAsync(4, timeout: 1))
            events.Add(evt);

        var e = Assert.Single(events);
        Assert.Equal(PaymentStatus.Pending, e.Data.Status);
        Assert.Equal(PaymentStage.Retrying, e.Data.Stage);
        Assert.StartsWith("Not sent yet;", e.Data.StatusDetail);
        Assert.Equal(new DateTimeOffset(2026, 10, 25, 14, 43, 53, TimeSpan.Zero), e.Data.RetryUntil);
    }

    [Fact]
    public async Task WatchAsync_KeepsUnknownStage()
    {
        var sse =
            "event: timeout\n" +
            "data: {\"number\":5,\"status\":\"processing\",\"amount\":1000,\"max_fee\":20,\"service_fee\":3,\"address\":\"x4k7m2@ln.bot\",\"stage\":\"teleporting\",\"status_detail\":\"Moving through a new kind of route.\",\"retry_until\":null}\n\n";
        var (client, _) = TestHelper.CreateSseClient(sse);

        var events = new List<PaymentEvent>();
        await foreach (var evt in client.Wallet("wal_1").Payments.WatchAsync(5, timeout: 1))
            events.Add(evt);

        var e = Assert.Single(events);
        Assert.Equal("teleporting", e.Data.Stage);
        Assert.Equal(PaymentStatus.Processing, e.Data.Status);
    }

    [Fact]
    public async Task WatchAsync_DecodesCamelCasePayload()
    {
        var sse =
            "event: settled\n" +
            "data: {\"number\":1,\"status\":\"settled\",\"amount\":50,\"maxFee\":10,\"serviceFee\":1,\"actualFee\":2,\"address\":\"user@ln.bot\",\"txNumber\":4,\"failureReason\":null,\"settledAt\":\"2026-10-24T14:50:14Z\"}\n\n";
        var (client, _) = TestHelper.CreateSseClient(sse);

        var events = new List<PaymentEvent>();
        await foreach (var evt in client.Wallet("wal_1").Payments.WatchAsync(1))
            events.Add(evt);

        var e = Assert.Single(events);
        Assert.Equal(10, e.Data.MaxFee);
        Assert.Equal(1, e.Data.ServiceFee);
        Assert.Equal(2, e.Data.ActualFee);
        Assert.Equal(4, e.Data.TxNumber);
        Assert.Equal(SsePayloads.SettledAt, e.Data.SettledAt);
    }

    [Fact]
    public async Task WatchByHashAsync_DecodesSnakeCaseSettledEvent()
    {
        var sse = $"event: settled\ndata: {SsePayloads.SettledPayment}\n\n";
        var (client, _) = TestHelper.CreateSseClient(sse);

        var events = new List<PaymentEvent>();
        await foreach (var evt in client.Wallet("wal_1").Payments.WatchByHashAsync("hash123"))
            events.Add(evt);

        var e = Assert.Single(events);
        Assert.Equal(3, e.Data.ActualFee);
        Assert.Equal(1, e.Data.ServiceFee);
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
    public async Task StreamAsync_DecodesSnakeCaseEnvelope()
    {
        var sse = $"data: {{\"event\":\"payment.settled\",\"created_at\":\"2026-10-24T14:50:14.123+00:00\",\"data\":{SsePayloads.SettledPayment}}}\n\n";
        var (client, _) = TestHelper.CreateSseClient(sse);

        var events = new List<WalletEvent>();
        await foreach (var evt in client.Wallet("wal_1").Events.StreamAsync())
            events.Add(evt);

        var e = Assert.Single(events);
        Assert.Equal("payment.settled", e.Event);
        Assert.Equal(new DateTimeOffset(2026, 10, 24, 14, 50, 14, 123, TimeSpan.Zero), e.CreatedAt);
        Assert.Equal(3, e.Data.GetProperty("actualFee").GetInt64());

        var payment = e.Data.Deserialize<PaymentResponse>(SsePayloads.JsonOptions)!;
        Assert.Equal(PaymentStatus.Settled, payment.Status);
        Assert.Equal(5, payment.MaxFee);
        Assert.Equal(1, payment.ServiceFee);
        Assert.Equal(3, payment.ActualFee);
        Assert.Equal(4, payment.TxNumber);
        Assert.Null(payment.FailureReason);
        Assert.Equal(SsePayloads.PaymentCreatedAt, payment.CreatedAt);
        Assert.Equal(SsePayloads.SettledAt, payment.SettledAt);
    }

    [Fact]
    public async Task StreamAsync_DecodesPartialPaymentCreated()
    {
        var sse = "data: {\"event\":\"payment.created\",\"created_at\":\"2026-10-24T14:50:01.456+00:00\",\"data\":{\"number\":1,\"status\":\"pending\",\"amount\":20,\"max_fee\":5,\"service_fee\":1,\"address\":\"bub@bub.gg\",\"reference\":null,\"created_at\":\"2026-10-24T14:50:01Z\"}}\n\n";
        var (client, _) = TestHelper.CreateSseClient(sse);

        var events = new List<WalletEvent>();
        await foreach (var evt in client.Wallet("wal_1").Events.StreamAsync())
            events.Add(evt);

        var e = Assert.Single(events);
        Assert.Equal("payment.created", e.Event);

        var payment = e.Data.Deserialize<PaymentResponse>(SsePayloads.JsonOptions)!;
        Assert.Equal(PaymentStatus.Pending, payment.Status);
        Assert.Equal(20, payment.Amount);
        Assert.Equal(5, payment.MaxFee);
        Assert.Equal(1, payment.ServiceFee);
        Assert.Equal("bub@bub.gg", payment.Address);
        Assert.Equal(SsePayloads.PaymentCreatedAt, payment.CreatedAt);
        Assert.Null(payment.ActualFee);
        Assert.Null(payment.Preimage);
        Assert.Null(payment.TxNumber);
        Assert.Null(payment.FailureReason);
        Assert.Null(payment.SettledAt);
    }

    [Fact]
    public async Task StreamAsync_DecodesInvoiceEvents()
    {
        var sse =
            "data: {\"event\":\"invoice.created\",\"created_at\":\"2026-10-24T14:49:00.789+00:00\",\"data\":{\"number\":7,\"status\":\"pending\",\"amount\":1000,\"bolt11\":\"lnbc10u1p...\",\"reference\":null,\"memo\":null,\"created_at\":\"2026-10-24T14:49:00Z\",\"expires_at\":\"2026-10-24T15:49:00Z\"}}\n\n" +
            $"data: {{\"event\":\"invoice.settled\",\"created_at\":\"2026-10-24T14:50:14.123+00:00\",\"data\":{SsePayloads.SettledInvoice}}}\n\n";
        var (client, _) = TestHelper.CreateSseClient(sse);

        var events = new List<WalletEvent>();
        await foreach (var evt in client.Wallet("wal_1").Events.StreamAsync())
            events.Add(evt);

        Assert.Equal(2, events.Count);

        Assert.Equal("invoice.created", events[0].Event);
        var created = events[0].Data.Deserialize<InvoiceResponse>(SsePayloads.JsonOptions)!;
        Assert.Equal(InvoiceStatus.Pending, created.Status);
        Assert.Equal(SsePayloads.InvoiceCreatedAt, created.CreatedAt);
        Assert.Equal(SsePayloads.InvoiceExpiresAt, created.ExpiresAt);
        Assert.Null(created.TxNumber);
        Assert.Null(created.SettledAt);

        Assert.Equal("invoice.settled", events[1].Event);
        var settled = events[1].Data.Deserialize<InvoiceResponse>(SsePayloads.JsonOptions)!;
        Assert.Equal(InvoiceStatus.Settled, settled.Status);
        Assert.Equal(4, settled.TxNumber);
        Assert.Equal(SsePayloads.SettledAt, settled.SettledAt);
    }

    [Fact]
    public async Task StreamAsync_DecodesCamelCaseEnvelope()
    {
        var sse = "data: {\"event\":\"invoice.settled\",\"createdAt\":\"2024-01-01T00:00:00Z\",\"data\":{\"number\":1,\"status\":\"settled\",\"amount\":100,\"bolt11\":\"lnbc1...\",\"txNumber\":2}}\n\n";
        var (client, _) = TestHelper.CreateSseClient(sse);

        var events = new List<WalletEvent>();
        await foreach (var evt in client.Wallet("wal_1").Events.StreamAsync())
            events.Add(evt);

        var e = Assert.Single(events);
        Assert.Equal("invoice.settled", e.Event);
        Assert.Equal(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero), e.CreatedAt);
        Assert.Equal(2, e.Data.Deserialize<InvoiceResponse>(SsePayloads.JsonOptions)!.TxNumber);
    }

    [Fact]
    public async Task StreamAsync_SkipsKeepaliveCommentsAndUsesEnvelopeEventName()
    {
        var sse =
            ":keepalive\n\n" +
            "event: ignored\n" +
            $"data:{{\"event\":\"payment.settled\",\"created_at\":\"2026-10-24T14:50:14.123+00:00\",\"data\":{SsePayloads.SettledPayment}}}\n\n" +
            ":keepalive\n\n";
        var (client, _) = TestHelper.CreateSseClient(sse);

        var events = new List<WalletEvent>();
        await foreach (var evt in client.Wallet("wal_1").Events.StreamAsync())
            events.Add(evt);

        Assert.Equal("payment.settled", Assert.Single(events).Event);
    }

    [Fact]
    public async Task StreamAsync_SkipsUndecodableEvent()
    {
        var sse = "data: {\"event\":\"payment.settled\",\"data\":{}}\n\n"
            + "data: {\"event\":\"payment.settled\",\"created_at\":\"2026-10-24T14:50:14Z\",\"data\":{\"number\":1}}\n\n";
        var (client, _) = TestHelper.CreateSseClient(sse);

        var events = new List<WalletEvent>();
        await foreach (var evt in client.Wallet("wal_1").Events.StreamAsync())
            events.Add(evt);

        Assert.Equal("payment.settled", Assert.Single(events).Event);
    }

    [Fact]
    public async Task StreamAsync_BuildsCorrectPath()
    {
        var (client, handler) = TestHelper.CreateSseClient("");

        await foreach (var _ in client.Wallet("wal_1").Events.StreamAsync()) { }

        Assert.EndsWith("/v1/wallets/wal_1/events", handler.LastRequest!.RequestUri!.AbsolutePath);
    }
}
