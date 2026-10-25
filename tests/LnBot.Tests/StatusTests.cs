using System.Net;
using LnBot.Exceptions;
using LnBot.Models;
using LnBot.Resources;
using Xunit;

namespace LnBot.Tests;

public class ServiceStatusTests
{
    private const string TooManyRequests = "{\"error\":\"Too many requests. Please try again later.\"}";

    private static async Task<ServiceStatusResponse> GetStatusFromFixtureAsync(string fixture)
    {
        var (client, handler) = TestHelper.CreateClientWithoutKey();
        handler.SetRawResponse(TestHelper.Fixture(fixture));
        return await client.GetStatusAsync();
    }

    [Fact]
    public async Task GetStatusAsync_GetsStatusWithoutApiKey()
    {
        var (client, handler) = TestHelper.CreateClientWithoutKey();
        handler.SetRawResponse(TestHelper.Fixture("status-operational.json"));

        await client.GetStatusAsync();

        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Equal("/v1/status", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Empty(handler.LastRequest.RequestUri.Query);
        Assert.Null(handler.LastRequest.Headers.Authorization);
        Assert.False(handler.LastRequest.Headers.Contains("Authorization"));
    }

    [Fact]
    public async Task GetStatusAsync_SendsApiKeyWhenSet()
    {
        var (client, handler) = TestHelper.CreateClient("uk_abc");
        handler.SetRawResponse(TestHelper.Fixture("status-operational.json"));

        await client.GetStatusAsync();

        Assert.Equal("Bearer", handler.LastRequest!.Headers.Authorization?.Scheme);
        Assert.Equal("uk_abc", handler.LastRequest.Headers.Authorization?.Parameter);
    }

    [Fact]
    public async Task GetStatusAsync_DecodesOperational()
    {
        var s = await GetStatusFromFixtureAsync("status-operational.json");

        Assert.Equal(ServiceState.Operational, s.Status);
        Assert.Equal(new DateTimeOffset(2026, 10, 25, 14, 33, 53, 60, TimeSpan.Zero), s.UpdatedAt);
        Assert.Null(s.Notice);

        var sending = s.Components.Sending;
        Assert.Equal(ServiceState.Operational, sending.Status);
        Assert.Equal(1, sending.InProgress);
        Assert.Equal(new Dictionary<string, int> { [PaymentStage.InFlight] = 1 }, sending.ByStage);
        Assert.Equal(3, sending.LongestWaitSeconds);
        Assert.Equal(42, sending.SentLastHour);
        Assert.Equal(1, sending.FailedLastHour);
        Assert.Equal(0, sending.ServiceFailuresLastHour);
        Assert.Equal(2.1, sending.MedianSeconds);
        Assert.Equal(6.8, sending.P95Seconds);

        Assert.Equal(ServiceState.Operational, s.Components.Receiving.Status);
        Assert.Equal(97, s.Components.Receiving.CreditedLastHour);
        Assert.Equal(0, s.Components.Receiving.CreditBacklog);

        Assert.Equal(ServiceState.Operational, s.Components.Node.Status);
        Assert.Equal(284, s.Components.Node.LatencyMs);

        Assert.Equal(ServiceState.Operational, s.Components.Webhooks.Status);
        Assert.Equal(0, s.Components.Webhooks.Backlog);
    }

    [Fact]
    public async Task GetStatusAsync_DecodesOutageWithNotice()
    {
        var s = await GetStatusFromFixtureAsync("status-outage.json");

        Assert.Equal(ServiceState.Outage, s.Status);

        var notice = Assert.IsType<ServiceNotice>(s.Notice);
        Assert.Equal(NoticeLevel.Outage, notice.Level);
        Assert.Equal("Payments are paused while the node recovers.", notice.Message);
        Assert.Equal(new DateTimeOffset(2026, 10, 25, 14, 10, 0, TimeSpan.Zero), notice.Since);
        Assert.Null(notice.Until);

        var sending = s.Components.Sending;
        Assert.Equal(ServiceState.Outage, sending.Status);
        Assert.Equal(6, sending.InProgress);
        Assert.Equal(new Dictionary<string, int> { [PaymentStage.Queued] = 2, [PaymentStage.Retrying] = 4 }, sending.ByStage);
        Assert.Equal(1260, sending.LongestWaitSeconds);
        Assert.Equal(5, sending.ServiceFailuresLastHour);
        Assert.Equal(3.0, sending.P95Seconds);

        Assert.Equal(ServiceState.Outage, s.Components.Receiving.Status);
        Assert.Equal(ServiceState.Outage, s.Components.Node.Status);
        Assert.Null(s.Components.Node.LatencyMs);
        Assert.Equal(ServiceState.Operational, s.Components.Webhooks.Status);
    }

    [Fact]
    public async Task GetStatusAsync_DecodesStaleUnknown()
    {
        var s = await GetStatusFromFixtureAsync("status-unknown-stale.json");

        Assert.Equal(ServiceState.Unknown, s.Status);
        Assert.Equal(new DateTimeOffset(2026, 10, 25, 13, 2, 11, TimeSpan.Zero), s.UpdatedAt);
        Assert.Equal(ServiceState.Unknown, s.Components.Sending.Status);
        Assert.Empty(s.Components.Sending.ByStage);
        Assert.Null(s.Components.Sending.LongestWaitSeconds);
        Assert.Equal(12, s.Components.Sending.SentLastHour);
        Assert.Equal(2.0, s.Components.Sending.MedianSeconds);
        Assert.Equal(30, s.Components.Receiving.CreditedLastHour);
        Assert.Equal(250, s.Components.Node.LatencyMs);
        Assert.Equal(ServiceState.Unknown, s.Components.Webhooks.Status);
    }

    [Fact]
    public async Task GetStatusAsync_DecodesNeverMeasured()
    {
        var s = await GetStatusFromFixtureAsync("status-never-measured.json");

        Assert.Equal(ServiceState.Unknown, s.Status);
        Assert.Null(s.UpdatedAt);
        Assert.Null(s.Notice);
        Assert.Equal(0, s.Components.Sending.InProgress);
        Assert.Empty(s.Components.Sending.ByStage);
        Assert.Null(s.Components.Sending.LongestWaitSeconds);
        Assert.Null(s.Components.Sending.MedianSeconds);
        Assert.Null(s.Components.Sending.P95Seconds);
        Assert.Null(s.Components.Receiving.CreditBacklog);
        Assert.Null(s.Components.Node.LatencyMs);
        Assert.Null(s.Components.Webhooks.Backlog);
    }

    [Fact]
    public async Task GetStatusAsync_DecodesLiveResponseWithSevenFractionalDigits()
    {
        var s = await GetStatusFromFixtureAsync("status-live.json");

        Assert.Equal(ServiceState.Operational, s.Status);
        Assert.Equal(new DateTimeOffset(2026, 10, 25, 17, 16, 11, TimeSpan.Zero).AddTicks(9284738), s.UpdatedAt);
        Assert.Empty(s.Components.Sending.ByStage);
        Assert.Null(s.Components.Sending.MedianSeconds);
        Assert.Equal(885, s.Components.Node.LatencyMs);
    }

    [Fact]
    public async Task GetStatusAsync_KeepsUnknownValuesAndIgnoresUnknownFields()
    {
        var (client, handler) = TestHelper.CreateClientWithoutKey();
        handler.SetRawResponse("""
            {
              "status": "maintenance",
              "updatedAt": "2026-10-25T14:33:53.06+00:00",
              "region": "eu",
              "notice": { "level": "critical", "message": "Planned work.", "since": "2026-10-25T14:00:00+00:00", "until": null, "link": "https://ln.bot/status" },
              "components": {
                "sending": { "status": "paused", "inProgress": 2, "byStage": { "teleporting": 2 }, "longestWaitSeconds": 12.5,
                             "sentLastHour": 0, "failedLastHour": 0, "serviceFailuresLastHour": 0, "medianSeconds": null, "p95Seconds": null, "newGauge": 7 },
                "receiving": { "status": "operational", "creditedLastHour": 1, "creditBacklog": 0 },
                "node": { "status": "operational", "latencyMs": 300 },
                "webhooks": { "status": "operational", "backlog": 0 },
                "swaps": { "status": "operational" }
              }
            }
            """);

        var s = await client.GetStatusAsync();

        Assert.Equal("maintenance", s.Status);
        Assert.Equal("critical", s.Notice!.Level);
        Assert.Equal("Planned work.", s.Notice.Message);
        Assert.Equal("paused", s.Components.Sending.Status);
        Assert.Equal(2, s.Components.Sending.ByStage["teleporting"]);
        Assert.Equal(12.5, s.Components.Sending.LongestWaitSeconds);
    }

    [Fact]
    public async Task GetStatusAsync_DecodesPartialResponse()
    {
        var (client, handler) = TestHelper.CreateClientWithoutKey();
        handler.SetRawResponse("{\"status\":\"operational\"}");

        var s = await client.GetStatusAsync();

        Assert.Equal(ServiceState.Operational, s.Status);
        Assert.Null(s.UpdatedAt);
        Assert.Null(s.Notice);
        Assert.NotNull(s.Components.Sending);
        Assert.Empty(s.Components.Sending.ByStage);
        Assert.Null(s.Components.Node.LatencyMs);
    }

    [Fact]
    public async Task GetStatusAsync_Throws429WithApiMessage()
    {
        var (client, handler) = TestHelper.CreateClientWithoutKey();
        handler.SetRawResponse(TooManyRequests, HttpStatusCode.TooManyRequests);

        var ex = await Assert.ThrowsAsync<TooManyRequestsException>(() => client.GetStatusAsync());

        Assert.Equal(429, ex.StatusCode);
        Assert.Equal("Too many requests. Please try again later.", ex.Message);
        Assert.Equal(TooManyRequests, ex.Body);
        Assert.IsAssignableFrom<LnBotException>(ex);
    }
}

public class ServiceStatusHistoryTests
{
    private static async Task<ServiceStatusHistoryResponse> GetHistoryFromFixtureAsync(string fixture, string range)
    {
        var (client, handler) = TestHelper.CreateClientWithoutKey();
        handler.SetRawResponse(TestHelper.Fixture(fixture));
        return await client.GetStatusHistoryAsync(range);
    }

    [Fact]
    public async Task GetStatusHistoryAsync_DefaultsTo24hWithoutApiKey()
    {
        var (client, handler) = TestHelper.CreateClientWithoutKey();
        handler.SetRawResponse(TestHelper.Fixture("history-24h-live.json"));

        await client.GetStatusHistoryAsync();

        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Equal("/v1/status/history", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Equal("?range=24h", handler.LastRequest.RequestUri.Query);
        Assert.Null(handler.LastRequest.Headers.Authorization);
        Assert.False(handler.LastRequest.Headers.Contains("Authorization"));
    }

    [Theory]
    [InlineData(StatusRange.Last24Hours, "24h")]
    [InlineData(StatusRange.Last7Days, "7d")]
    [InlineData(StatusRange.Last30Days, "30d")]
    [InlineData(StatusRange.Last90Days, "90d")]
    public async Task GetStatusHistoryAsync_PassesRange(string range, string expected)
    {
        var (client, handler) = TestHelper.CreateClientWithoutKey();
        handler.SetRawResponse("{}");

        await client.GetStatusHistoryAsync(range);

        Assert.Equal($"?range={expected}", handler.LastRequest!.RequestUri!.Query);
    }

    [Fact]
    public async Task GetStatusHistoryAsync_SendsOtherRangesAsIs()
    {
        var (client, handler) = TestHelper.CreateClientWithoutKey();
        handler.SetRawResponse("{}");

        await client.GetStatusHistoryAsync("1y");
        Assert.Equal("?range=1y", handler.LastRequest!.RequestUri!.Query);

        await client.GetStatusHistoryAsync("a b&c");
        Assert.Equal("?range=a%20b%26c", handler.LastRequest!.RequestUri!.Query);
    }

    [Fact]
    public async Task GetStatusHistoryAsync_ThrowsForNullRange()
    {
        var (client, _) = TestHelper.CreateClientWithoutKey();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.GetStatusHistoryAsync(null!));
    }

    [Fact]
    public async Task GetStatusHistoryAsync_ThrowsBadRequestForUnsupportedRange()
    {
        var (client, handler) = TestHelper.CreateClientWithoutKey();
        handler.SetRawResponse("{\"error\":\"range must be 24h, 7d, 30d or 90d.\"}", HttpStatusCode.BadRequest);

        var ex = await Assert.ThrowsAsync<BadRequestException>(() => client.GetStatusHistoryAsync("1y"));

        Assert.Equal("range must be 24h, 7d, 30d or 90d.", ex.Message);
    }

    [Fact]
    public async Task GetStatusHistoryAsync_Throws429WithApiMessage()
    {
        var (client, handler) = TestHelper.CreateClientWithoutKey();
        handler.SetRawResponse("{\"error\":\"Too many requests. Please try again later.\"}", HttpStatusCode.TooManyRequests);

        var ex = await Assert.ThrowsAsync<TooManyRequestsException>(() => client.GetStatusHistoryAsync(StatusRange.Last7Days));

        Assert.Equal(429, ex.StatusCode);
        Assert.Equal("Too many requests. Please try again later.", ex.Message);
    }

    [Fact]
    public async Task GetStatusHistoryAsync_Decodes90dSummary()
    {
        var h = await GetHistoryFromFixtureAsync("history-90d.json", StatusRange.Last90Days);

        Assert.Equal(StatusRange.Last90Days, h.Range);
        Assert.Equal(86400, h.BucketSeconds);
        Assert.Equal(new DateTimeOffset(2026, 7, 27, 0, 0, 0, TimeSpan.Zero), h.From);
        Assert.Equal(new DateTimeOffset(2026, 10, 25, 14, 35, 12, TimeSpan.Zero), h.To);

        var sum = h.Summary;
        Assert.Equal(2850, sum.Samples);
        Assert.Equal(99.123, sum.Uptime.Overall.Operational);
        Assert.Equal(0, sum.Uptime.Overall.Degraded);
        Assert.Equal(0.877, sum.Uptime.Overall.Outage);
        Assert.Equal(0.877, sum.Uptime.Sending.Outage);
        Assert.Equal(0.877, sum.Uptime.Receiving.Outage);
        Assert.Equal(0.877, sum.Uptime.Node.Outage);
        Assert.Equal(100, sum.Uptime.Webhooks.Operational);
        Assert.Equal(2410, sum.Sent);
        Assert.Equal(31, sum.Failed);
        Assert.Equal(6, sum.ServiceFailures);
        Assert.Equal(5012, sum.Credited);
        Assert.Equal(2.2, sum.MedianSeconds);
        Assert.Equal(7.9, sum.P95Seconds);
        Assert.Equal(301, sum.AvgLatencyMs);
        Assert.Equal(20000, sum.MaxLatencyMs);

        Assert.Equal(
            new double?[] { 1, 2, 3, 5, 10, 20, 30, 60, 120, 300, 600, null },
            sum.SettleTimes.Select(t => t.UpToSeconds));
        Assert.Equal(
            new[] { 70, 1100, 690, 300, 160, 50, 20, 12, 5, 2, 1, 0 },
            sum.SettleTimes.Select(t => t.Count));
    }

    [Fact]
    public async Task GetStatusHistoryAsync_Decodes90dBuckets()
    {
        var h = await GetHistoryFromFixtureAsync("history-90d.json", StatusRange.Last90Days);

        Assert.Equal(3, h.Buckets.Count);

        var measured = h.Buckets[0];
        Assert.Equal(new DateTimeOffset(2026, 10, 23, 0, 0, 0, TimeSpan.Zero), measured.At);
        Assert.Equal(1425, measured.Samples);
        Assert.Equal(0, measured.Overall.Outage);
        Assert.Equal(1200, measured.Sending.Sent);
        Assert.Equal(12, measured.Sending.Failed);
        Assert.Equal(1, measured.Sending.ServiceFailures);
        Assert.Equal(2.2, measured.Sending.MedianSeconds);
        Assert.Equal(7.5, measured.Sending.P95Seconds);
        Assert.Equal(4, measured.Sending.MaxInProgress);
        Assert.Equal(41, measured.Sending.MaxWaitSeconds);
        Assert.Equal(2500, measured.Receiving.Credited);
        Assert.Equal(3, measured.Receiving.MaxBacklog);
        Assert.Equal(290, measured.Node.AvgLatencyMs);
        Assert.Equal(1900, measured.Node.MaxLatencyMs);
        Assert.Equal(4, measured.Webhooks.MaxBacklog);

        var outage = h.Buckets[1];
        Assert.Equal(25, outage.Overall.Outage);
        Assert.Equal(0, outage.Overall.Degraded);
        // Every part reads the same way, so the caller can compute uptime per part.
        StatusBucketPart[] parts = [outage.Overall, outage.Sending, outage.Receiving, outage.Node, outage.Webhooks];
        Assert.Equal(new[] { 25, 25, 25, 25, 0 }, parts.Select(p => p.Outage));
        Assert.Equal(1500, outage.Sending.MaxWaitSeconds);
        Assert.Equal(40, outage.Receiving.MaxBacklog);
        Assert.Equal(20000, outage.Node.MaxLatencyMs);
    }

    [Fact]
    public async Task GetStatusHistoryAsync_DecodesUnmeasuredBucketWithNulls()
    {
        var h = await GetHistoryFromFixtureAsync("history-90d.json", StatusRange.Last90Days);

        var b = h.Buckets[2];
        Assert.Equal(new DateTimeOffset(2026, 10, 25, 0, 0, 0, TimeSpan.Zero), b.At);
        Assert.Equal(0, b.Samples);
        Assert.Equal(0, b.Overall.Degraded);
        Assert.Equal(0, b.Overall.Outage);
        Assert.Equal(0, b.Sending.Sent);
        Assert.Null(b.Sending.MedianSeconds);
        Assert.Null(b.Sending.P95Seconds);
        Assert.Equal(0, b.Sending.MaxInProgress);
        Assert.Null(b.Sending.MaxWaitSeconds);
        Assert.Equal(0, b.Receiving.Credited);
        Assert.Null(b.Receiving.MaxBacklog);
        Assert.Null(b.Node.AvgLatencyMs);
        Assert.Null(b.Node.MaxLatencyMs);
        Assert.Null(b.Webhooks.MaxBacklog);
    }

    [Fact]
    public async Task GetStatusHistoryAsync_Decodes90dNotices()
    {
        var h = await GetHistoryFromFixtureAsync("history-90d.json", StatusRange.Last90Days);

        Assert.Equal(2, h.Notices.Count);
        Assert.Equal(NoticeLevel.Outage, h.Notices[0].Level);
        Assert.Equal("Payments are paused while the node recovers.", h.Notices[0].Message);
        Assert.Equal(new DateTimeOffset(2026, 10, 24, 9, 5, 0, TimeSpan.Zero), h.Notices[0].Since);
        Assert.Equal(new DateTimeOffset(2026, 10, 24, 9, 32, 0, TimeSpan.Zero), h.Notices[0].Until);
        Assert.Equal(NoticeLevel.Info, h.Notices[1].Level);
        Assert.Equal(new DateTimeOffset(2026, 10, 24, 11, 0, 0, TimeSpan.Zero), h.Notices[1].Until);
    }

    [Fact]
    public async Task GetStatusHistoryAsync_DecodesLive24h()
    {
        var h = await GetHistoryFromFixtureAsync("history-24h-live.json", StatusRange.Last24Hours);

        Assert.Equal(StatusRange.Last24Hours, h.Range);
        Assert.Equal(300, h.BucketSeconds);
        Assert.Equal(new DateTimeOffset(2026, 10, 24, 17, 15, 0, TimeSpan.Zero), h.From);
        Assert.Equal(new DateTimeOffset(2026, 10, 25, 17, 16, 44, TimeSpan.Zero).AddTicks(1635789), h.To);
        Assert.Empty(h.Notices);

        // A full range plus the current bucket, still filling, each 5 minutes after the one before.
        Assert.Equal(289, h.Buckets.Count);
        Assert.Equal(h.From, h.Buckets[0].At);
        for (var i = 1; i < h.Buckets.Count; i++)
            Assert.Equal(TimeSpan.FromSeconds(h.BucketSeconds), h.Buckets[i].At - h.Buckets[i - 1].At);

        Assert.Equal(h.Summary.Samples, h.Buckets.Sum(b => b.Samples));
        Assert.Equal(h.Summary.Sent, h.Buckets.Sum(b => b.Sending.Sent));
        Assert.Equal(12, h.Summary.SettleTimes.Count);
        Assert.Equal(9.7, h.Summary.MedianSeconds);
        Assert.Equal(780, h.Summary.AvgLatencyMs);
        Assert.Equal(1715, h.Summary.MaxLatencyMs);
        Assert.Equal(100, h.Summary.Uptime.Overall.Operational);

        var unmeasured = h.Buckets.Where(b => b.Samples == 0).ToList();
        Assert.NotEmpty(unmeasured);
        Assert.All(unmeasured, b =>
        {
            Assert.Null(b.Sending.MedianSeconds);
            Assert.Null(b.Sending.P95Seconds);
            Assert.Null(b.Sending.MaxWaitSeconds);
            Assert.Null(b.Receiving.MaxBacklog);
            Assert.Null(b.Node.AvgLatencyMs);
            Assert.Null(b.Node.MaxLatencyMs);
            Assert.Null(b.Webhooks.MaxBacklog);
        });
        Assert.All(h.Buckets.Where(b => b.Samples > 0), b => Assert.NotNull(b.Node.AvgLatencyMs));
    }

    [Fact]
    public async Task GetStatusHistoryAsync_KeepsUnknownValuesAndNullGauges()
    {
        var (client, handler) = TestHelper.CreateClientWithoutKey();
        handler.SetRawResponse("""
            {
              "range": "1y", "bucketSeconds": 604800, "granularity": "week",
              "from": "2025-10-25T00:00:00+00:00", "to": "2026-10-25T14:35:12.1234567+00:00",
              "summary": {
                "samples": 0,
                "uptime": { "overall": { "operational": null, "degraded": null, "outage": null } },
                "sent": 0, "failed": 0, "serviceFailures": 0, "credited": 0,
                "medianSeconds": null, "p95Seconds": null, "settleTimes": [], "avgLatencyMs": null, "maxLatencyMs": null
              },
              "buckets": [
                {
                  "at": "2026-10-19T00:00:00+00:00", "samples": 3,
                  "overall": { "degraded": 1, "outage": 0 },
                  "sending": { "degraded": 1, "outage": 0, "sent": 0, "failed": 0, "serviceFailures": 0,
                               "medianSeconds": null, "p95Seconds": null, "maxInProgress": null, "maxWaitSeconds": null },
                  "receiving": { "degraded": 0, "outage": 0, "credited": 0, "maxBacklog": null },
                  "node": { "degraded": 0, "outage": 0, "avgLatencyMs": 296.67, "maxLatencyMs": 340 },
                  "webhooks": { "degraded": 0, "outage": 0, "maxBacklog": null },
                  "swaps": { "degraded": 0, "outage": 0 }
                }
              ],
              "notices": [ { "level": "maintenance", "message": "Planned work.", "since": "2026-10-20T09:00:00+00:00", "until": null } ]
            }
            """);

        var h = await client.GetStatusHistoryAsync("1y");

        Assert.Equal("1y", h.Range);
        Assert.Equal(604800, h.BucketSeconds);
        Assert.Null(h.Summary.Uptime.Overall.Operational);
        Assert.Null(h.Summary.Uptime.Overall.Degraded);
        Assert.Null(h.Summary.Uptime.Overall.Outage);
        Assert.Null(h.Summary.Uptime.Sending.Operational);
        Assert.Empty(h.Summary.SettleTimes);
        Assert.Null(h.Summary.AvgLatencyMs);

        var b = Assert.Single(h.Buckets);
        Assert.Equal(1, b.Sending.Degraded);
        Assert.Null(b.Sending.MaxInProgress);
        Assert.Equal(296.67, b.Node.AvgLatencyMs);

        var notice = Assert.Single(h.Notices);
        Assert.Equal("maintenance", notice.Level);
        Assert.Null(notice.Until);
    }
}

public class StatusInterfaceTests
{
    [Fact]
    public async Task StatusCalls_AreReachableThroughTheInterface()
    {
        var (client, handler) = TestHelper.CreateClientWithoutKey();
        ILnBotClient api = client;

        handler.SetRawResponse(TestHelper.Fixture("status-operational.json"));
        Assert.Equal(ServiceState.Operational, (await api.GetStatusAsync()).Status);
        Assert.Equal("/v1/status", handler.LastRequest!.RequestUri!.AbsolutePath);

        handler.SetRawResponse(TestHelper.Fixture("history-90d.json"));
        await api.GetStatusHistoryAsync();
        Assert.Equal("?range=24h", handler.LastRequest!.RequestUri!.Query);
    }

    [Fact]
    public async Task ImplementationWrittenBefore120_StillCompilesAndThrowsNotSupported()
    {
        ILnBotClient api = new ClientWithoutStatusCalls();

        await Assert.ThrowsAsync<NotSupportedException>(() => api.GetStatusAsync());
        await Assert.ThrowsAsync<NotSupportedException>(() => api.GetStatusHistoryAsync());
    }

    /// <summary>An implementation of the interface as it was in 1.1.1, without the status calls.</summary>
    private sealed class ClientWithoutStatusCalls : ILnBotClient
    {
        public WalletsResource Wallets => throw new NotImplementedException();
        public KeysResource Keys => throw new NotImplementedException();
        public PublicInvoicesResource Invoices => throw new NotImplementedException();
        public BackupResource Backup => throw new NotImplementedException();
        public RestoreResource Restore => throw new NotImplementedException();
        public Task<RegisterResponse> RegisterAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<MeResponse> MeAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public WalletScope Wallet(string walletId) => throw new NotImplementedException();
        public void Dispose() { }
    }
}
