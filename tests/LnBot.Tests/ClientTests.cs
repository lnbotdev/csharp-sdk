using System.Net;
using System.Text.Json;
using LnBot.Exceptions;
using LnBot.Models;
using Xunit;

namespace LnBot.Tests;

public class ClientTests
{
    [Fact]
    public void Constructor_InitializesAllResources()
    {
        var (client, _) = TestHelper.CreateClient();
        Assert.NotNull(client.Wallets);
        Assert.NotNull(client.Keys);
        Assert.NotNull(client.Invoices);
        Assert.NotNull(client.Payments);
        Assert.NotNull(client.Addresses);
        Assert.NotNull(client.Transactions);
        Assert.NotNull(client.Webhooks);
        Assert.NotNull(client.Events);
        Assert.NotNull(client.Backup);
        Assert.NotNull(client.Restore);
        Assert.NotNull(client.L402);
    }

    [Fact]
    public async Task SendsAuthorizationHeader()
    {
        var (client, handler) = TestHelper.CreateClient("key_abc");
        handler.SetResponse(new { walletId = "wal_1", name = "Test", balance = 0, onHold = 0, available = 0 });

        await client.Wallets.CurrentAsync();

        Assert.NotNull(handler.LastRequest);
        Assert.Equal("Bearer", handler.LastRequest.Headers.Authorization?.Scheme);
        Assert.Equal("key_abc", handler.LastRequest.Headers.Authorization?.Parameter);
    }

    [Fact]
    public async Task OmitsAuthorizationHeaderWhenNoApiKey()
    {
        var handler = new MockHandler();
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.ln.bot") };
        var client = new LnBotClient(null, new LnBotClientOptions { HttpClient = http });

        handler.SetResponse(new { walletId = "wal_1", primaryKey = "k", secondaryKey = "k", name = "n", address = "a", recoveryPassphrase = "p" });
        await client.Wallets.CreateAsync();

        Assert.Null(handler.LastRequest!.Headers.Authorization);
    }

    [Fact]
    public async Task SendsUserAgentHeader()
    {
        var (client, handler) = TestHelper.CreateClient();
        handler.SetResponse(new { walletId = "wal_1", name = "Test", balance = 0, onHold = 0, available = 0 });

        await client.Wallets.CurrentAsync();

        var ua = handler.LastRequest!.Headers.UserAgent.ToString();
        Assert.Contains("lnbot-csharp/", ua);
    }

    [Fact]
    public async Task SendsJsonContentType_ForPostWithBody()
    {
        var (client, handler) = TestHelper.CreateClient();
        handler.SetResponse(new { number = 1, status = "pending", amount = 100, bolt11 = "lnbc1..." });

        await client.Invoices.CreateAsync(new CreateInvoiceRequest { Amount = 100 });

        Assert.NotNull(handler.LastRequest!.Content);
        Assert.Equal("application/json", handler.LastRequest.Content!.Headers.ContentType!.MediaType);
    }

    [Fact]
    public async Task SerializesBodyAsCamelCaseJson()
    {
        var (client, handler) = TestHelper.CreateClient();
        handler.SetResponse(new { number = 1, status = "pending", amount = 100, bolt11 = "lnbc1..." });

        await client.Invoices.CreateAsync(new CreateInvoiceRequest { Amount = 100, Memo = "test memo" });

        var body = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.True(body.RootElement.TryGetProperty("amount", out _));
        Assert.True(body.RootElement.TryGetProperty("memo", out _));
        Assert.Equal(100, body.RootElement.GetProperty("amount").GetInt64());
        Assert.Equal("test memo", body.RootElement.GetProperty("memo").GetString());
    }

    [Fact]
    public async Task OmitsNullFieldsInBody()
    {
        var (client, handler) = TestHelper.CreateClient();
        handler.SetResponse(new { number = 1, status = "pending", amount = 100, bolt11 = "lnbc1..." });

        await client.Invoices.CreateAsync(new CreateInvoiceRequest { Amount = 100 });

        var body = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.False(body.RootElement.TryGetProperty("memo", out _));
        Assert.False(body.RootElement.TryGetProperty("reference", out _));
    }

    [Fact]
    public async Task ParsesJsonResponse()
    {
        var (client, handler) = TestHelper.CreateClient();
        handler.SetResponse(new { walletId = "wal_123", name = "My Wallet", balance = 1000, onHold = 50, available = 950 });

        var wallet = await client.Wallets.CurrentAsync();

        Assert.Equal("wal_123", wallet.WalletId);
        Assert.Equal("My Wallet", wallet.Name);
        Assert.Equal(1000, wallet.Balance);
        Assert.Equal(50, wallet.OnHold);
        Assert.Equal(950, wallet.Available);
    }

    [Fact]
    public async Task UsesGetMethod_ForGetOperations()
    {
        var (client, handler) = TestHelper.CreateClient();
        handler.SetResponse(new { walletId = "wal_1", name = "n", balance = 0, onHold = 0, available = 0 });

        await client.Wallets.CurrentAsync();

        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
    }

    [Fact]
    public async Task UsesPostMethod_ForCreateOperations()
    {
        var (client, handler) = TestHelper.CreateClient();
        handler.SetResponse(new { number = 1, status = "pending", amount = 100, bolt11 = "lnbc1..." });

        await client.Invoices.CreateAsync(new CreateInvoiceRequest { Amount = 100 });

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
    }

    [Fact]
    public async Task UsesPatchMethod_ForUpdateOperations()
    {
        var (client, handler) = TestHelper.CreateClient();
        handler.SetResponse(new { walletId = "wal_1", name = "New", balance = 0, onHold = 0, available = 0 });

        await client.Wallets.UpdateAsync(new UpdateWalletRequest { Name = "New" });

        Assert.Equal(HttpMethod.Patch, handler.LastRequest!.Method);
    }

    [Fact]
    public async Task UsesDeleteMethod_ForDeleteOperations()
    {
        var (client, handler) = TestHelper.CreateClient();
        handler.SetRawResponse("", HttpStatusCode.NoContent);

        await client.Webhooks.DeleteAsync("wh_123");

        Assert.Equal(HttpMethod.Delete, handler.LastRequest!.Method);
    }

    // ── Error mapping ──

    [Fact]
    public async Task Throws_BadRequestException_For400()
    {
        var (client, handler) = TestHelper.CreateClient();
        handler.SetRawResponse("{\"message\":\"invalid amount\"}", HttpStatusCode.BadRequest);

        var ex = await Assert.ThrowsAsync<BadRequestException>(() => client.Wallets.CurrentAsync());
        Assert.Equal("invalid amount", ex.Message);
        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task Throws_UnauthorizedException_For401()
    {
        var (client, handler) = TestHelper.CreateClient();
        handler.SetRawResponse("{\"message\":\"bad key\"}", HttpStatusCode.Unauthorized);

        await Assert.ThrowsAsync<UnauthorizedException>(() => client.Wallets.CurrentAsync());
    }

    [Fact]
    public async Task Throws_ForbiddenException_For403()
    {
        var (client, handler) = TestHelper.CreateClient();
        handler.SetRawResponse("{\"message\":\"denied\"}", HttpStatusCode.Forbidden);

        await Assert.ThrowsAsync<ForbiddenException>(() => client.Wallets.CurrentAsync());
    }

    [Fact]
    public async Task Throws_NotFoundException_For404()
    {
        var (client, handler) = TestHelper.CreateClient();
        handler.SetRawResponse("{\"message\":\"not found\"}", HttpStatusCode.NotFound);

        await Assert.ThrowsAsync<NotFoundException>(() => client.Wallets.CurrentAsync());
    }

    [Fact]
    public async Task Throws_ConflictException_For409()
    {
        var (client, handler) = TestHelper.CreateClient();
        handler.SetRawResponse("{\"message\":\"conflict\"}", HttpStatusCode.Conflict);

        await Assert.ThrowsAsync<ConflictException>(() => client.Wallets.CurrentAsync());
    }

    [Fact]
    public async Task Throws_LnBotException_ForOtherStatusCodes()
    {
        var (client, handler) = TestHelper.CreateClient();
        handler.SetRawResponse("{\"error\":\"server error\"}", HttpStatusCode.InternalServerError);

        var ex = await Assert.ThrowsAsync<LnBotException>(() => client.Wallets.CurrentAsync());
        Assert.Equal(500, ex.StatusCode);
    }

    [Fact]
    public async Task ExtractsMessageFromJsonError()
    {
        var (client, handler) = TestHelper.CreateClient();
        handler.SetRawResponse("{\"message\":\"invalid amount\"}", HttpStatusCode.BadRequest);

        var ex = await Assert.ThrowsAsync<BadRequestException>(() => client.Wallets.CurrentAsync());
        Assert.Equal("invalid amount", ex.Message);
    }

    [Fact]
    public async Task ExtractsErrorFieldFromJsonError()
    {
        var (client, handler) = TestHelper.CreateClient();
        handler.SetRawResponse("{\"error\":\"bad input\"}", HttpStatusCode.BadRequest);

        var ex = await Assert.ThrowsAsync<BadRequestException>(() => client.Wallets.CurrentAsync());
        Assert.Equal("bad input", ex.Message);
    }

    [Fact]
    public async Task ErrorBody_IsIncludedInException()
    {
        var (client, handler) = TestHelper.CreateClient();
        handler.SetRawResponse("{\"message\":\"fail\",\"details\":\"extra\"}", HttpStatusCode.BadRequest);

        var ex = await Assert.ThrowsAsync<BadRequestException>(() => client.Wallets.CurrentAsync());
        Assert.Contains("extra", ex.Body);
    }

    // ── Dispose ──

    [Fact]
    public void Dispose_DoesNotDisposeInjectedHttpClient()
    {
        var handler = new MockHandler();
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.ln.bot") };
        var client = new LnBotClient("key_test", new LnBotClientOptions { HttpClient = http });

        client.Dispose();

        // If HttpClient was disposed, this would throw
        handler.SetResponse(new { walletId = "wal_1", name = "n", balance = 0, onHold = 0, available = 0 });
        Assert.NotNull(http.GetAsync("https://api.ln.bot/v1/wallets/current"));
    }
}
