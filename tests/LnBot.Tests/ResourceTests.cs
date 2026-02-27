using System.Net;
using System.Text.Json;
using LnBot.Models;
using Xunit;

namespace LnBot.Tests;

public class WalletsResourceTests
{
    [Fact]
    public async Task CreateAsync_PostsToWallets()
    {
        var (client, handler) = TestHelper.CreateClient();
        handler.SetResponse(new { walletId = "wal_1", primaryKey = "k1", secondaryKey = "k2", name = "n", address = "a", recoveryPassphrase = "p" });

        var result = await client.Wallets.CreateAsync(new CreateWalletRequest { Name = "Test" });

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.EndsWith("/v1/wallets", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Equal("wal_1", result.WalletId);
    }

    [Fact]
    public async Task CreateAsync_WorksWithoutArgs()
    {
        var (client, handler) = TestHelper.CreateClient();
        handler.SetResponse(new { walletId = "wal_1", primaryKey = "k1", secondaryKey = "k2", name = "n", address = "a", recoveryPassphrase = "p" });

        await client.Wallets.CreateAsync();

        Assert.Null(handler.LastRequestBody);
    }

    [Fact]
    public async Task CurrentAsync_GetsWalletsCurrent()
    {
        var (client, handler) = TestHelper.CreateClient();
        handler.SetResponse(new { walletId = "wal_1", name = "n", balance = 100, onHold = 0, available = 100 });

        await client.Wallets.CurrentAsync();

        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.EndsWith("/v1/wallets/current", handler.LastRequest.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task UpdateAsync_PatchesWalletsCurrent()
    {
        var (client, handler) = TestHelper.CreateClient();
        handler.SetResponse(new { walletId = "wal_1", name = "New", balance = 0, onHold = 0, available = 0 });

        await client.Wallets.UpdateAsync(new UpdateWalletRequest { Name = "New" });

        Assert.Equal(HttpMethod.Patch, handler.LastRequest!.Method);
        var body = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.Equal("New", body.RootElement.GetProperty("name").GetString());
    }
}

public class KeysResourceTests
{
    [Fact]
    public async Task RotateAsync_PostsToKeysSlotRotate()
    {
        var (client, handler) = TestHelper.CreateClient();
        handler.SetResponse(new { key = "key_new", name = "primary" });

        await client.Keys.RotateAsync(0);

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.EndsWith("/v1/keys/0/rotate", handler.LastRequest.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task RotateAsync_EncodesSlotInPath()
    {
        var (client, handler) = TestHelper.CreateClient();
        handler.SetResponse(new { key = "key_new", name = "secondary" });

        await client.Keys.RotateAsync(1);

        Assert.EndsWith("/v1/keys/1/rotate", handler.LastRequest!.RequestUri!.AbsolutePath);
    }
}

public class InvoicesResourceTests
{
    [Fact]
    public async Task CreateAsync_PostsToInvoices()
    {
        var (client, handler) = TestHelper.CreateClient();
        handler.SetResponse(new { number = 1, status = "pending", amount = 100, bolt11 = "lnbc1..." });

        var result = await client.Invoices.CreateAsync(new CreateInvoiceRequest { Amount = 100, Memo = "test" });

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.EndsWith("/v1/invoices", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Equal(1, result.Number);
    }

    [Fact]
    public async Task ListAsync_GetsInvoices()
    {
        var (client, handler) = TestHelper.CreateClient();
        handler.SetRawResponse("[]");

        await client.Invoices.ListAsync();

        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.EndsWith("/v1/invoices", handler.LastRequest.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task ListAsync_PassesPaginationParams()
    {
        var (client, handler) = TestHelper.CreateClient();
        handler.SetRawResponse("[]");

        await client.Invoices.ListAsync(new PaginationParams { Limit = 10, After = 5 });

        var query = handler.LastRequest!.RequestUri!.Query;
        Assert.Contains("limit=10", query);
        Assert.Contains("after=5", query);
    }

    [Fact]
    public async Task ListAsync_OmitsNullPaginationParams()
    {
        var (client, handler) = TestHelper.CreateClient();
        handler.SetRawResponse("[]");

        await client.Invoices.ListAsync(new PaginationParams { Limit = 10 });

        var query = handler.LastRequest!.RequestUri!.Query;
        Assert.Contains("limit=10", query);
        Assert.DoesNotContain("after", query);
    }

    [Fact]
    public async Task GetAsync_GetsInvoiceByNumber()
    {
        var (client, handler) = TestHelper.CreateClient();
        handler.SetResponse(new { number = 42, status = "settled", amount = 100, bolt11 = "lnbc1..." });

        await client.Invoices.GetAsync(42);

        Assert.EndsWith("/v1/invoices/42", handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task GetByHashAsync_GetsInvoiceByHash()
    {
        var (client, handler) = TestHelper.CreateClient();
        handler.SetResponse(new { number = 1, status = "settled", amount = 100, bolt11 = "lnbc1..." });

        await client.Invoices.GetByHashAsync("abc123");

        Assert.EndsWith("/v1/invoices/abc123", handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task CreateForWalletAsync_PostsToInvoicesForWallet()
    {
        var (client, handler) = TestHelper.CreateClient();
        handler.SetResponse(new { bolt11 = "lnbc1...", amount = 50, expiresAt = "2099-01-01T00:00:00Z" });

        await client.Invoices.CreateForWalletAsync(new CreateInvoiceForWalletRequest { WalletId = "wal_1", Amount = 50 });

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.EndsWith("/v1/invoices/for-wallet", handler.LastRequest.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task CreateForAddressAsync_PostsToInvoicesForAddress()
    {
        var (client, handler) = TestHelper.CreateClient();
        handler.SetResponse(new { bolt11 = "lnbc1...", amount = 50, expiresAt = "2099-01-01T00:00:00Z" });

        await client.Invoices.CreateForAddressAsync(new CreateInvoiceForAddressRequest { Address = "user@ln.bot", Amount = 50 });

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.EndsWith("/v1/invoices/for-address", handler.LastRequest.RequestUri!.AbsolutePath);
    }
}

public class PaymentsResourceTests
{
    [Fact]
    public async Task CreateAsync_PostsToPayments()
    {
        var (client, handler) = TestHelper.CreateClient();
        handler.SetResponse(new { number = 1, status = "settled", amount = 100, maxFee = 10, serviceFee = 0, address = "user@ln.bot" });

        await client.Payments.CreateAsync(new CreatePaymentRequest { Target = "user@ln.bot", Amount = 100 });

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.EndsWith("/v1/payments", handler.LastRequest.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task ListAsync_GetsPayments()
    {
        var (client, handler) = TestHelper.CreateClient();
        handler.SetRawResponse("[]");

        await client.Payments.ListAsync();

        Assert.EndsWith("/v1/payments", handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task ListAsync_PassesPaginationParams()
    {
        var (client, handler) = TestHelper.CreateClient();
        handler.SetRawResponse("[]");

        await client.Payments.ListAsync(new PaginationParams { Limit = 5, After = 10 });

        var query = handler.LastRequest!.RequestUri!.Query;
        Assert.Contains("limit=5", query);
        Assert.Contains("after=10", query);
    }

    [Fact]
    public async Task GetAsync_GetsPaymentByNumber()
    {
        var (client, handler) = TestHelper.CreateClient();
        handler.SetResponse(new { number = 7, status = "settled", amount = 50, maxFee = 10, serviceFee = 0, address = "a" });

        await client.Payments.GetAsync(7);

        Assert.EndsWith("/v1/payments/7", handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task GetByHashAsync_GetsPaymentByHash()
    {
        var (client, handler) = TestHelper.CreateClient();
        handler.SetResponse(new { number = 1, status = "settled", amount = 50, maxFee = 10, serviceFee = 0, address = "a" });

        await client.Payments.GetByHashAsync("hash123");

        Assert.EndsWith("/v1/payments/hash123", handler.LastRequest!.RequestUri!.AbsolutePath);
    }
}

public class AddressesResourceTests
{
    [Fact]
    public async Task CreateAsync_PostsToAddresses()
    {
        var (client, handler) = TestHelper.CreateClient();
        handler.SetResponse(new { address = "random@ln.bot", generated = true, cost = 0 });

        await client.Addresses.CreateAsync();

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.EndsWith("/v1/addresses", handler.LastRequest.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task CreateAsync_WithVanityAddress()
    {
        var (client, handler) = TestHelper.CreateClient();
        handler.SetResponse(new { address = "vanity@ln.bot", generated = false, cost = 100 });

        await client.Addresses.CreateAsync(new CreateAddressRequest { Address = "vanity" });

        var body = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.Equal("vanity", body.RootElement.GetProperty("address").GetString());
    }

    [Fact]
    public async Task ListAsync_GetsAddresses()
    {
        var (client, handler) = TestHelper.CreateClient();
        handler.SetRawResponse("[]");

        await client.Addresses.ListAsync();

        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.EndsWith("/v1/addresses", handler.LastRequest.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task DeleteAsync_DeletesAddress()
    {
        var (client, handler) = TestHelper.CreateClient();
        handler.SetRawResponse("", HttpStatusCode.NoContent);

        await client.Addresses.DeleteAsync("test@ln.bot");

        Assert.Equal(HttpMethod.Delete, handler.LastRequest!.Method);
        Assert.Contains("/v1/addresses/test%40ln.bot", handler.LastRequest.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task TransferAsync_PostsToTransfer()
    {
        var (client, handler) = TestHelper.CreateClient();
        handler.SetResponse(new { address = "test@ln.bot", transferredTo = "wal_2" });

        await client.Addresses.TransferAsync("test@ln.bot", new TransferAddressRequest { TargetWalletKey = "key_target" });

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/v1/addresses/test%40ln.bot/transfer", handler.LastRequest.RequestUri!.AbsolutePath);
    }
}

public class TransactionsResourceTests
{
    [Fact]
    public async Task ListAsync_GetsTransactions()
    {
        var (client, handler) = TestHelper.CreateClient();
        handler.SetRawResponse("[]");

        await client.Transactions.ListAsync();

        Assert.EndsWith("/v1/transactions", handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task ListAsync_PassesPaginationParams()
    {
        var (client, handler) = TestHelper.CreateClient();
        handler.SetRawResponse("[]");

        await client.Transactions.ListAsync(new PaginationParams { Limit = 20, After = 3 });

        var query = handler.LastRequest!.RequestUri!.Query;
        Assert.Contains("limit=20", query);
        Assert.Contains("after=3", query);
    }
}

public class WebhooksResourceTests
{
    [Fact]
    public async Task CreateAsync_PostsToWebhooks()
    {
        var (client, handler) = TestHelper.CreateClient();
        handler.SetResponse(new { id = "wh_1", url = "https://example.com/hook", secret = "sec" });

        await client.Webhooks.CreateAsync(new CreateWebhookRequest { Url = "https://example.com/hook" });

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.EndsWith("/v1/webhooks", handler.LastRequest.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task ListAsync_GetsWebhooks()
    {
        var (client, handler) = TestHelper.CreateClient();
        handler.SetRawResponse("[]");

        await client.Webhooks.ListAsync();

        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.EndsWith("/v1/webhooks", handler.LastRequest.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task DeleteAsync_DeletesWebhook()
    {
        var (client, handler) = TestHelper.CreateClient();
        handler.SetRawResponse("", HttpStatusCode.NoContent);

        await client.Webhooks.DeleteAsync("wh_123");

        Assert.Equal(HttpMethod.Delete, handler.LastRequest!.Method);
        Assert.EndsWith("/v1/webhooks/wh_123", handler.LastRequest.RequestUri!.AbsolutePath);
    }
}

public class BackupResourceTests
{
    [Fact]
    public async Task RecoveryAsync_PostsToBackupRecovery()
    {
        var (client, handler) = TestHelper.CreateClient();
        handler.SetResponse(new { passphrase = "word1 word2 word3" });

        var result = await client.Backup.RecoveryAsync();

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.EndsWith("/v1/backup/recovery", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Equal("word1 word2 word3", result.Passphrase);
    }

    [Fact]
    public async Task PasskeyBeginAsync_PostsToBackupPasskeyBegin()
    {
        var (client, handler) = TestHelper.CreateClient();
        handler.SetResponse(new { sessionId = "s1", options = new { } });

        await client.Backup.PasskeyBeginAsync();

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.EndsWith("/v1/backup/passkey/begin", handler.LastRequest.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task PasskeyCompleteAsync_PostsToBackupPasskeyComplete()
    {
        var (client, handler) = TestHelper.CreateClient();
        handler.SetRawResponse("", HttpStatusCode.NoContent);

        await client.Backup.PasskeyCompleteAsync(new BackupPasskeyCompleteRequest
        {
            SessionId = "s1",
            Attestation = JsonSerializer.SerializeToElement(new { }),
        });

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.EndsWith("/v1/backup/passkey/complete", handler.LastRequest.RequestUri!.AbsolutePath);
    }
}

public class RestoreResourceTests
{
    [Fact]
    public async Task RecoveryAsync_PostsToRestoreRecovery()
    {
        var (client, handler) = TestHelper.CreateClient();
        handler.SetResponse(new { walletId = "wal_1", name = "n", primaryKey = "k1", secondaryKey = "k2" });

        await client.Restore.RecoveryAsync(new RecoveryRestoreRequest { Passphrase = "word1 word2 word3" });

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.EndsWith("/v1/restore/recovery", handler.LastRequest.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task PasskeyBeginAsync_PostsToRestorePasskeyBegin()
    {
        var (client, handler) = TestHelper.CreateClient();
        handler.SetResponse(new { sessionId = "s1", options = new { } });

        await client.Restore.PasskeyBeginAsync();

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.EndsWith("/v1/restore/passkey/begin", handler.LastRequest.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task PasskeyCompleteAsync_PostsToRestorePasskeyComplete()
    {
        var (client, handler) = TestHelper.CreateClient();
        handler.SetResponse(new { walletId = "wal_1", name = "n", primaryKey = "k1", secondaryKey = "k2" });

        await client.Restore.PasskeyCompleteAsync(new RestorePasskeyCompleteRequest
        {
            SessionId = "s1",
            Assertion = JsonSerializer.SerializeToElement(new { }),
        });

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.EndsWith("/v1/restore/passkey/complete", handler.LastRequest.RequestUri!.AbsolutePath);
    }
}

public class L402ResourceTests
{
    [Fact]
    public async Task CreateChallengeAsync_PostsToL402Challenges()
    {
        var (client, handler) = TestHelper.CreateClient();
        handler.SetResponse(new { macaroon = "mac", invoice = "lnbc1...", paymentHash = "h", expiresAt = "2099-01-01T00:00:00Z", wwwAuthenticate = "L402 ..." });

        await client.L402.CreateChallengeAsync(new CreateL402ChallengeRequest { Amount = 10, Description = "test" });

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.EndsWith("/v1/l402/challenges", handler.LastRequest.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task VerifyAsync_PostsToL402Verify()
    {
        var (client, handler) = TestHelper.CreateClient();
        handler.SetResponse(new { valid = true, paymentHash = "h" });

        await client.L402.VerifyAsync(new VerifyL402Request { Authorization = "L402 mac:pre" });

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.EndsWith("/v1/l402/verify", handler.LastRequest.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task PayAsync_PostsToL402Pay()
    {
        var (client, handler) = TestHelper.CreateClient();
        handler.SetResponse(new { authorization = "L402 mac:pre", paymentHash = "h", preimage = "pre", amount = 10, fee = 0, paymentNumber = 1, status = "settled" });

        await client.L402.PayAsync(new PayL402Request { WwwAuthenticate = "L402 macaroon=\"mac\", invoice=\"inv\"" });

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.EndsWith("/v1/l402/pay", handler.LastRequest.RequestUri!.AbsolutePath);
    }
}
