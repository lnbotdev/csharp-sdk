/// <summary>
/// Integration tests for the LnBot .NET SDK.
///
/// These tests hit the live API with real sats. They validate every SDK method,
/// response shapes, error handling, balance bookkeeping, and edge cases.
///
/// Requires env vars:
///   LNBOT_USER_KEY   — user key (uk_...) that owns the prefunded wallet
///   LNBOT_WALLET_ID  — wallet ID (wal_...) of the prefunded wallet
///
/// Run: dotnet test --filter "Category=Integration"
/// </summary>

using LnBot;
using LnBot.Exceptions;
using LnBot.Models;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace LnBot.Tests;

[Trait("Category", "Integration")]
[TestCaseOrderer("LnBot.Tests.PriorityOrderer", "LnBot.Tests")]
public class IntegrationTests : IAsyncLifetime
{
    private static readonly string? UserKey = Environment.GetEnvironmentVariable("LNBOT_USER_KEY");
    private static readonly string? WalletId = Environment.GetEnvironmentVariable("LNBOT_WALLET_ID");

    // Shared state across tests
    private static LnBotClient? s_client;
    private static WalletScope? s_w1;
    private static long s_w1BalanceBefore;
    private static CreateWalletResponse? s_w2Info;
    private static WalletKeyResponse? s_w2Key;
    private static LnBotClient? s_w2Client;
    private static AddressResponse? s_w2Address;
    private static InvoiceResponse? s_w2Invoice;
    private static PaymentResponse? s_w1Payment;
    private static bool s_initialized;
    private static bool s_skipped;

    public async Task InitializeAsync()
    {
        if (UserKey is null || WalletId is null)
        {
            s_skipped = true;
            return;
        }

        if (!s_initialized)
        {
            s_client = new LnBotClient(UserKey);
            s_w1 = s_client.Wallet(WalletId!);

            var w1Info = await s_w1.GetAsync();
            s_w1BalanceBefore = w1Info.Balance;

            s_w2Info = await s_client.Wallets.CreateAsync();
            s_w2Key = await s_client.Wallet(s_w2Info.WalletId).Key.CreateAsync();
            s_w2Client = new LnBotClient(s_w2Key.Key);

            s_initialized = true;
        }
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static bool IsSkipped => s_skipped;

    private static async Task<PaymentResponse> WaitForPayment(WalletScope wallet, int paymentNumber)
    {
        for (int i = 0; i < 30; i++)
        {
            var p = await wallet.Payments.GetAsync(paymentNumber);
            if (p.Status is PaymentStatus.Settled or PaymentStatus.Failed) return p;
            await Task.Delay(500);
        }
        return await wallet.Payments.GetAsync(paymentNumber);
    }

    // =========================================================================
    // ACCOUNT
    // =========================================================================

    [Fact, TestPriority(100)]
    public async Task Register_CreatesNewAccount()
    {
        if (IsSkipped) return;
        var noAuth = new LnBotClient();
        var account = await noAuth.RegisterAsync();
        Assert.StartsWith("usr_", account.UserId);
        Assert.NotEmpty(account.PrimaryKey);
        Assert.NotEmpty(account.SecondaryKey);
        Assert.Equal(12, account.RecoveryPassphrase.Split(' ').Length);
    }

    [Fact, TestPriority(101)]
    public async Task Me_ReturnsIdentityWithUserKey()
    {
        if (IsSkipped) return;
        var me = await s_client!.MeAsync();
        Assert.NotNull(me);
    }

    [Fact, TestPriority(102)]
    public async Task Me_ReturnsIdentityWithWalletKey()
    {
        if (IsSkipped) return;
        var me = await s_w2Client!.MeAsync();
        Assert.NotNull(me.WalletId);
        Assert.NotEmpty(me.WalletId!);
    }

    [Fact, TestPriority(103)]
    public async Task Me_RejectsInvalidKey()
    {
        if (IsSkipped) return;
        var bad = new LnBotClient("uk_invalid");
        await Assert.ThrowsAsync<UnauthorizedException>(() => bad.MeAsync());
    }

    // =========================================================================
    // WALLETS
    // =========================================================================

    [Fact, TestPriority(200)]
    public async Task WalletsCreate_ReturnsWalletWithAddress()
    {
        if (IsSkipped) return;
        Assert.StartsWith("wal_", s_w2Info!.WalletId);
        Assert.NotEmpty(s_w2Info.Name);
        Assert.Contains("@", s_w2Info.Address);
    }

    [Fact, TestPriority(201)]
    public async Task WalletsList_IncludesBothWallets()
    {
        if (IsSkipped) return;
        var wallets = await s_client!.Wallets.ListAsync();
        var ids = wallets.Select(w => w.WalletId).ToList();
        Assert.Contains(WalletId, ids);
        Assert.Contains(s_w2Info!.WalletId, ids);

        var item = wallets.First(w => w.WalletId == s_w2Info.WalletId);
        Assert.NotEmpty(item.Name);
        Assert.NotNull(item.CreatedAt);
    }

    [Fact, TestPriority(202)]
    public async Task WalletGet_ReturnsFullBalanceInfo()
    {
        if (IsSkipped) return;
        var info = await s_w1!.GetAsync();
        Assert.Equal(WalletId, info.WalletId);
        Assert.True(info.Balance > 0);
        Assert.True(info.Available >= 0);
        Assert.True(info.Available <= info.Balance);
    }

    [Fact, TestPriority(203)]
    public async Task WalletUpdate_ChangesName()
    {
        if (IsSkipped) return;
        var name = $"test-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
        var updated = await s_w1!.UpdateAsync(new UpdateWalletRequest { Name = name });
        Assert.Equal(name, updated.Name);
        Assert.Equal(WalletId, updated.WalletId);

        var fetched = await s_w1.GetAsync();
        Assert.Equal(name, fetched.Name);
    }

    [Fact, TestPriority(204)]
    public async Task WalletGet_RejectsNonexistentWallet()
    {
        if (IsSkipped) return;
        var bad = s_client!.Wallet("wal_nonexistent");
        await Assert.ThrowsAsync<NotFoundException>(() => bad.GetAsync());
    }

    // =========================================================================
    // WALLET KEYS
    // =========================================================================

    [Fact, TestPriority(300)]
    public void WalletKeyCreate_ReturnedWkKey()
    {
        if (IsSkipped) return;
        Assert.StartsWith("wk_", s_w2Key!.Key);
        Assert.NotEmpty(s_w2Key.Hint);
    }

    [Fact, TestPriority(301)]
    public async Task WalletKeyCreate_RejectsDuplicate()
    {
        if (IsSkipped) return;
        await Assert.ThrowsAnyAsync<LnBotException>(
            () => s_client!.Wallet(s_w2Info!.WalletId).Key.CreateAsync());
    }

    [Fact, TestPriority(302)]
    public async Task WalletKeyGet_ReturnsMetadata()
    {
        if (IsSkipped) return;
        var info = await s_client!.Wallet(s_w2Info!.WalletId).Key.GetAsync();
        Assert.NotEmpty(info.Hint);
        Assert.NotNull(info.CreatedAt);
    }

    [Fact, TestPriority(303)]
    public async Task WalletKeyRotate_ReturnsNewKey()
    {
        if (IsSkipped) return;
        var rotated = await s_client!.Wallet(s_w2Info!.WalletId).Key.RotateAsync();
        Assert.StartsWith("wk_", rotated.Key);
        Assert.NotEqual(s_w2Key!.Key, rotated.Key);
        s_w2Key = rotated;
        s_w2Client = new LnBotClient(s_w2Key.Key);
    }

    [Fact, TestPriority(304)]
    public async Task WalletCurrentGet_WorksWithWalletKey()
    {
        if (IsSkipped) return;
        var info = await s_w2Client!.Wallet("current").GetAsync();
        Assert.Equal(s_w2Info!.WalletId, info.WalletId);
    }

    // =========================================================================
    // ADDRESSES
    // =========================================================================

    [Fact, TestPriority(400)]
    public async Task AddressesCreate_CreatesRandomAddress()
    {
        if (IsSkipped) return;
        var w2 = s_client!.Wallet(s_w2Info!.WalletId);
        s_w2Address = await w2.Addresses.CreateAsync();
        Assert.Contains("@", s_w2Address.Address);
        Assert.True(s_w2Address.Generated);
        Assert.Equal(0, s_w2Address.Cost);
        Assert.NotNull(s_w2Address.CreatedAt);
    }

    [Fact, TestPriority(401)]
    public async Task AddressesList_IncludesCreatedAddress()
    {
        if (IsSkipped) return;
        var w2 = s_client!.Wallet(s_w2Info!.WalletId);
        var addresses = await w2.Addresses.ListAsync();
        Assert.True(addresses.Count >= 1);
        var found = addresses.FirstOrDefault(a => a.Address == s_w2Address!.Address);
        Assert.NotNull(found);
        Assert.True(found!.Generated);
    }

    [Fact, TestPriority(402)]
    public async Task AddressesTransfer_RejectsGeneratedAddresses()
    {
        if (IsSkipped) return;
        var w2 = s_client!.Wallet(s_w2Info!.WalletId);
        var extra = await w2.Addresses.CreateAsync();

        WalletKeyResponse? w1Key;
        try
        {
            w1Key = await s_w1!.Key.CreateAsync();
        }
        catch
        {
            w1Key = await s_w1!.Key.RotateAsync();
        }

        await Assert.ThrowsAsync<BadRequestException>(
            () => w2.Addresses.TransferAsync(extra.Address, new TransferAddressRequest { TargetWalletKey = w1Key.Key }));

        await w2.Addresses.DeleteAsync(extra.Address);
        await s_w1!.Key.DeleteAsync();
    }

    // =========================================================================
    // INVOICES
    // =========================================================================

    [Fact, TestPriority(500)]
    public async Task InvoicesCreate_WithAllFields()
    {
        if (IsSkipped) return;
        var w2 = s_client!.Wallet(s_w2Info!.WalletId);
        s_w2Invoice = await w2.Invoices.CreateAsync(new CreateInvoiceRequest
        {
            Amount = 2,
            Memo = "sdk-test",
            Reference = "ref-001",
        });
        Assert.True(s_w2Invoice.Number > 0);
        Assert.Equal(InvoiceStatus.Pending, s_w2Invoice.Status);
        Assert.StartsWith("lnbc", s_w2Invoice.Bolt11);
        Assert.Equal(2, s_w2Invoice.Amount);
        Assert.Equal("sdk-test", s_w2Invoice.Memo);
        Assert.Equal("ref-001", s_w2Invoice.Reference);
        Assert.Null(s_w2Invoice.Preimage);
        Assert.Null(s_w2Invoice.TxNumber);
        Assert.NotNull(s_w2Invoice.CreatedAt);
        Assert.Null(s_w2Invoice.SettledAt);
        Assert.NotNull(s_w2Invoice.ExpiresAt);
    }

    [Fact, TestPriority(501)]
    public async Task InvoicesCreate_RejectsZeroAmount()
    {
        if (IsSkipped) return;
        var w2 = s_client!.Wallet(s_w2Info!.WalletId);
        await Assert.ThrowsAsync<BadRequestException>(
            () => w2.Invoices.CreateAsync(new CreateInvoiceRequest { Amount = 0 }));
    }

    [Fact, TestPriority(502)]
    public async Task InvoicesList_ReturnsArrayWithPagination()
    {
        if (IsSkipped) return;
        var w2 = s_client!.Wallet(s_w2Info!.WalletId);
        var all = await w2.Invoices.ListAsync(new PaginationParams { Limit = 10 });
        Assert.True(all.Count >= 1);

        if (all.Count >= 2)
        {
            var page = await w2.Invoices.ListAsync(new PaginationParams { Limit = 1, After = all[0].Number });
            Assert.True(page.Count >= 1);
            Assert.True(page[0].Number < all[0].Number);
        }
    }

    [Fact, TestPriority(503)]
    public async Task InvoicesGet_ByNumber()
    {
        if (IsSkipped) return;
        var w2 = s_client!.Wallet(s_w2Info!.WalletId);
        var inv = await w2.Invoices.GetAsync(s_w2Invoice!.Number);
        Assert.Equal(s_w2Invoice.Number, inv.Number);
        Assert.Equal(2, inv.Amount);
        Assert.Equal("ref-001", inv.Reference);
    }

    [Fact, TestPriority(504)]
    public async Task InvoicesGet_RejectsNonexistentNumber()
    {
        if (IsSkipped) return;
        var w2 = s_client!.Wallet(s_w2Info!.WalletId);
        await Assert.ThrowsAsync<NotFoundException>(() => w2.Invoices.GetAsync(999999));
    }

    [Fact, TestPriority(505)]
    public async Task InvoicesCreateForWallet_WithoutAuth()
    {
        if (IsSkipped) return;
        var noAuth = new LnBotClient();
        var inv = await noAuth.Invoices.CreateForWalletAsync(new CreateInvoiceForWalletRequest
        {
            WalletId = s_w2Info!.WalletId,
            Amount = 5,
        });
        Assert.StartsWith("lnbc", inv.Bolt11);
        Assert.Equal(5, inv.Amount);
    }

    [Fact, TestPriority(506)]
    public async Task InvoicesCreateForAddress_WithoutAuth()
    {
        if (IsSkipped) return;
        var noAuth = new LnBotClient();
        var inv = await noAuth.Invoices.CreateForAddressAsync(new CreateInvoiceForAddressRequest
        {
            Address = s_w2Address!.Address,
            Amount = 5,
        });
        Assert.StartsWith("lnbc", inv.Bolt11);
        Assert.Equal(5, inv.Amount);
    }

    [Fact, TestPriority(507)]
    public async Task InvoicesCreateForWallet_RejectsNonexistentWallet()
    {
        if (IsSkipped) return;
        var noAuth = new LnBotClient();
        await Assert.ThrowsAsync<BadRequestException>(
            () => noAuth.Invoices.CreateForWalletAsync(new CreateInvoiceForWalletRequest
            {
                WalletId = "wal_nonexistent",
                Amount = 1,
            }));
    }

    // =========================================================================
    // PAYMENTS + BALANCE BOOKKEEPING
    // =========================================================================

    [Fact, TestPriority(600)]
    public async Task PaymentsResolve_LightningAddress()
    {
        if (IsSkipped) return;
        var resolved = await s_w1!.Payments.ResolveAsync(s_w2Address!.Address);
        Assert.Equal("lightning_address", resolved.Type);
        Assert.NotNull(resolved.Min);
        Assert.NotNull(resolved.Max);
        Assert.NotNull(resolved.Fixed);
    }

    [Fact, TestPriority(601)]
    public async Task PaymentsResolve_Bolt11Invoice()
    {
        if (IsSkipped) return;
        var resolved = await s_w1!.Payments.ResolveAsync(s_w2Invoice!.Bolt11);
        Assert.Equal("bolt11", resolved.Type);
        Assert.Equal(2, resolved.Amount);
        Assert.True(resolved.Fixed);
    }

    [Fact, TestPriority(602)]
    public async Task PaymentsCreate_PaysInvoiceAndSettles()
    {
        if (IsSkipped) return;
        s_w1Payment = await s_w1!.Payments.CreateAsync(new CreatePaymentRequest { Target = s_w2Invoice!.Bolt11 });
        Assert.True(s_w1Payment.Number > 0);
        Assert.Equal(2, s_w1Payment.Amount);

        var settled = await WaitForPayment(s_w1!, s_w1Payment.Number);
        Assert.Equal(PaymentStatus.Settled, settled.Status);
        Assert.NotNull(settled.Preimage);
        Assert.NotNull(settled.TxNumber);
        Assert.NotNull(settled.SettledAt);
        s_w1Payment = settled;
    }

    [Fact, TestPriority(603)]
    public async Task BalancesUpdatedCorrectlyAfterPayment()
    {
        if (IsSkipped) return;
        var w1After = await s_w1!.GetAsync();
        var w2After = await s_client!.Wallet(s_w2Info!.WalletId).GetAsync();

        // w1 lost amount + fees
        Assert.True(w1After.Balance < s_w1BalanceBefore);
        Assert.Equal(
            s_w1BalanceBefore - s_w1Payment!.Amount - s_w1Payment.ServiceFee - (s_w1Payment.ActualFee ?? 0),
            w1After.Balance);

        // w2 gained exactly the invoice amount
        Assert.Equal(2, w2After.Balance);
    }

    [Fact, TestPriority(604)]
    public async Task InvoiceIsSettledOnWallet2()
    {
        if (IsSkipped) return;
        var w2 = s_client!.Wallet(s_w2Info!.WalletId);
        var inv = await w2.Invoices.GetAsync(s_w2Invoice!.Number);
        Assert.Equal(InvoiceStatus.Settled, inv.Status);
        Assert.NotNull(inv.SettledAt);
    }

    [Fact, TestPriority(605)]
    public async Task PaymentsList_IncludesPaymentWithPagination()
    {
        if (IsSkipped) return;
        var payments = await s_w1!.Payments.ListAsync(new PaginationParams { Limit = 5 });
        Assert.Contains(payments, p => p.Number == s_w1Payment!.Number);

        if (payments.Count >= 2)
        {
            var page = await s_w1.Payments.ListAsync(new PaginationParams { Limit = 1, After = payments[0].Number });
            Assert.True(page.Count >= 1);
            Assert.True(page[0].Number < payments[0].Number);
        }
    }

    [Fact, TestPriority(606)]
    public async Task PaymentsGet_ByNumber()
    {
        if (IsSkipped) return;
        var payment = await s_w1!.Payments.GetAsync(s_w1Payment!.Number);
        Assert.Equal(s_w1Payment.Number, payment.Number);
        Assert.Equal(PaymentStatus.Settled, payment.Status);
        Assert.Equal(2, payment.Amount);
    }

    [Fact, TestPriority(607)]
    public async Task PaymentsGet_RejectsNonexistentNumber()
    {
        if (IsSkipped) return;
        await Assert.ThrowsAsync<NotFoundException>(() => s_w1!.Payments.GetAsync(999999));
    }

    [Fact, TestPriority(608)]
    public async Task PaymentsCreate_RejectsInsufficientBalance()
    {
        if (IsSkipped) return;
        var w2 = s_client!.Wallet(s_w2Info!.WalletId);
        var inv = await s_w1!.Invoices.CreateAsync(new CreateInvoiceRequest { Amount = 999999 });
        await Assert.ThrowsAsync<BadRequestException>(
            () => w2.Payments.CreateAsync(new CreatePaymentRequest { Target = inv.Bolt11 }));
    }

    [Fact, TestPriority(609)]
    public async Task PaymentsCreate_IdempotencyKeyPreventsDoublePay()
    {
        if (IsSkipped) return;
        var w2 = s_client!.Wallet(s_w2Info!.WalletId);
        var inv = await s_w1!.Invoices.CreateAsync(new CreateInvoiceRequest { Amount = 1 });
        var idempotencyKey = $"idem-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

        var p1 = await w2.Payments.CreateAsync(new CreatePaymentRequest
        {
            Target = inv.Bolt11,
            IdempotencyKey = idempotencyKey,
        });
        await WaitForPayment(w2, p1.Number);

        var p2 = await w2.Payments.CreateAsync(new CreatePaymentRequest
        {
            Target = inv.Bolt11,
            IdempotencyKey = idempotencyKey,
        });
        Assert.Equal(p1.Number, p2.Number);
    }

    // =========================================================================
    // TRANSACTIONS
    // =========================================================================

    [Fact, TestPriority(700)]
    public async Task TransactionsList_HasDebitEntryForPayment()
    {
        if (IsSkipped) return;
        var txns = await s_w1!.Transactions.ListAsync(new PaginationParams { Limit = 10 });
        Assert.True(txns.Count > 0);

        var debit = txns.FirstOrDefault(t => t.Type == TransactionType.Debit);
        Assert.NotNull(debit);
        Assert.True(debit!.Amount > 0);
        Assert.NotNull(debit.CreatedAt);
    }

    [Fact, TestPriority(701)]
    public async Task TransactionsList_HasCreditEntryOnWallet2()
    {
        if (IsSkipped) return;
        var w2 = s_client!.Wallet(s_w2Info!.WalletId);
        var txns = await w2.Transactions.ListAsync(new PaginationParams { Limit = 10 });
        var credit = txns.FirstOrDefault(t => t.Type == TransactionType.Credit);
        Assert.NotNull(credit);
        Assert.Equal(2, credit!.Amount);
    }

    [Fact, TestPriority(702)]
    public async Task TransactionsList_Pagination()
    {
        if (IsSkipped) return;
        var txns = await s_w1!.Transactions.ListAsync(new PaginationParams { Limit = 1 });
        Assert.Single(txns);

        if (txns.Count > 0)
        {
            var next = await s_w1.Transactions.ListAsync(new PaginationParams { Limit = 1, After = txns[0].Number });
            if (next.Count > 0)
            {
                Assert.True(next[0].Number < txns[0].Number);
            }
        }
    }

    // =========================================================================
    // WEBHOOKS
    // =========================================================================

    [Fact, TestPriority(800)]
    public async Task Webhooks_FullCrudCycle()
    {
        if (IsSkipped) return;
        var w2 = s_client!.Wallet(s_w2Info!.WalletId);

        var created = await w2.Webhooks.CreateAsync(new CreateWebhookRequest { Url = "https://example.com/hook" });
        Assert.NotEmpty(created.Id);
        Assert.NotEmpty(created.Secret);
        Assert.Equal("https://example.com/hook", created.Url);
        Assert.NotNull(created.CreatedAt);

        var list = await w2.Webhooks.ListAsync();
        var found = list.FirstOrDefault(wh => wh.Id == created.Id);
        Assert.NotNull(found);
        Assert.Equal("https://example.com/hook", found!.Url);

        await w2.Webhooks.DeleteAsync(created.Id);
        var listAfter = await w2.Webhooks.ListAsync();
        Assert.DoesNotContain(listAfter, wh => wh.Id == created.Id);
    }

    [Fact, TestPriority(801)]
    public async Task WebhooksDelete_RejectsNonexistentId()
    {
        if (IsSkipped) return;
        var w2 = s_client!.Wallet(s_w2Info!.WalletId);
        await Assert.ThrowsAsync<NotFoundException>(() => w2.Webhooks.DeleteAsync("nonexistent"));
    }

    // =========================================================================
    // L402 — FULL FLOW
    // =========================================================================

    [Fact, TestPriority(900)]
    public async Task L402CreateChallenge_ReturnsAllFields()
    {
        if (IsSkipped) return;
        var challenge = await s_w1!.L402.CreateChallengeAsync(new CreateL402ChallengeRequest
        {
            Amount = 1,
            Description = "test paywall",
            ExpirySeconds = 300,
            Caveats = ["service=test"],
        });
        Assert.NotEmpty(challenge.Macaroon);
        Assert.StartsWith("lnbc", challenge.Invoice);
        Assert.NotEmpty(challenge.PaymentHash);
        Assert.Contains("L402", challenge.WwwAuthenticate);
        Assert.Contains("macaroon=", challenge.WwwAuthenticate);
        Assert.Contains("invoice=", challenge.WwwAuthenticate);
    }

    [Fact, TestPriority(901)]
    public async Task L402Verify_RejectsInvalidToken()
    {
        if (IsSkipped) return;
        await Assert.ThrowsAsync<BadRequestException>(
            () => s_w1!.L402.VerifyAsync(new VerifyL402Request { Authorization = "L402 invalid:invalid" }));
    }

    [Fact, TestPriority(902)]
    public async Task L402FullFlow_ChallengePay_Verify()
    {
        if (IsSkipped) return;
        var challenge = await s_w1!.L402.CreateChallengeAsync(new CreateL402ChallengeRequest { Amount = 1 });

        var w2 = s_client!.Wallet(s_w2Info!.WalletId);
        var payResult = await w2.L402.PayAsync(new PayL402Request
        {
            WwwAuthenticate = challenge.WwwAuthenticate,
            MaxFee = 1,
            Wait = true,
            Timeout = 30,
        });
        Assert.Equal("settled", payResult.Status);
        Assert.NotNull(payResult.Authorization);
        Assert.Contains("L402", payResult.Authorization!);
        Assert.NotNull(payResult.Preimage);
        Assert.Equal(challenge.PaymentHash, payResult.PaymentHash);
        Assert.Equal(1, payResult.Amount);

        var verified = await s_w1!.L402.VerifyAsync(new VerifyL402Request { Authorization = payResult.Authorization! });
        Assert.True(verified.Valid);
        Assert.Equal(challenge.PaymentHash, verified.PaymentHash);
        Assert.NotNull(verified.Caveats);
        Assert.Null(verified.Error);
    }

    // =========================================================================
    // SSE: INVOICE WATCH
    // =========================================================================

    [Fact, TestPriority(1000)]
    public async Task InvoicesWatch_YieldsSettlementEvent()
    {
        if (IsSkipped) return;
        var w2 = s_client!.Wallet(s_w2Info!.WalletId);
        var inv = await w2.Invoices.CreateAsync(new CreateInvoiceRequest { Amount = 1, Memo = "watch-test" });

        var w2wk = s_w2Client!.Wallet(s_w2Info!.WalletId);
        var events = new List<InvoiceEvent>();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var watchTask = Task.Run(async () =>
        {
            await foreach (var evt in w2wk.Invoices.WatchAsync(inv.Number, timeout: 60, cancellationToken: cts.Token))
            {
                events.Add(evt);
                if (evt.Event is "settled" or "expired") break;
            }
        }, cts.Token);

        await Task.Delay(1500);
        await s_w1!.Payments.CreateAsync(new CreatePaymentRequest { Target = inv.Bolt11 });

        await watchTask;
        Assert.Contains(events, e => e.Event == "settled");
    }

    // =========================================================================
    // SSE: PAYMENT WATCH
    // =========================================================================

    [Fact, TestPriority(1001)]
    public async Task PaymentsWatch_YieldsSettlementEvent()
    {
        if (IsSkipped) return;
        var w2 = s_client!.Wallet(s_w2Info!.WalletId);
        var inv = await s_w1!.Invoices.CreateAsync(new CreateInvoiceRequest { Amount = 1 });

        var payment = await w2.Payments.CreateAsync(new CreatePaymentRequest { Target = inv.Bolt11 });

        var w2wk = s_w2Client!.Wallet(s_w2Info!.WalletId);
        var events = new List<PaymentEvent>();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await foreach (var evt in w2wk.Payments.WatchAsync(payment.Number, timeout: 30, cancellationToken: cts.Token))
        {
            events.Add(evt);
            if (evt.Event is "settled" or "failed") break;
        }

        Assert.Contains(events, e => e.Event == "settled");
    }

    // =========================================================================
    // SSE: WALLET EVENT STREAM
    // =========================================================================

    [Fact, TestPriority(1002)]
    public async Task EventsStream_ReceivesInvoiceAndPaymentEvents()
    {
        if (IsSkipped) return;
        var w2wk = s_w2Client!.Wallet(s_w2Info!.WalletId);
        var w2 = s_client!.Wallet(s_w2Info!.WalletId);

        var events = new List<WalletEvent>();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        var streamTask = Task.Run(async () =>
        {
            await foreach (var evt in w2wk.Events.StreamAsync(cts.Token))
            {
                events.Add(evt);
                if (events.Count >= 2) break;
            }
        }, cts.Token);

        await Task.Delay(1500);

        var inv = await w2.Invoices.CreateAsync(new CreateInvoiceRequest { Amount = 1 });
        await s_w1!.Payments.CreateAsync(new CreatePaymentRequest { Target = inv.Bolt11 });

        await Task.WhenAny(streamTask, Task.Delay(15_000));
        cts.Cancel();

        Assert.True(events.Count >= 1);
        Assert.Contains(events, e => e.Event.StartsWith("invoice."));
    }

    // =========================================================================
    // BACKUP
    // =========================================================================

    [Fact, TestPriority(1100)]
    public async Task BackupRecovery_Generates12WordPassphrase()
    {
        if (IsSkipped) return;
        var result = await s_client!.Backup.RecoveryAsync();
        Assert.NotEmpty(result.Passphrase);
        Assert.Equal(12, result.Passphrase.Split(' ').Length);
    }

    // =========================================================================
    // ERROR HANDLING
    // =========================================================================

    [Fact, TestPriority(1200)]
    public async Task RejectsUnauthenticatedAccess()
    {
        if (IsSkipped) return;
        var noAuth = new LnBotClient();
        await Assert.ThrowsAsync<UnauthorizedException>(() => noAuth.MeAsync());
    }

    [Fact, TestPriority(1201)]
    public async Task RejectsWrongWalletId()
    {
        if (IsSkipped) return;
        var bad = s_client!.Wallet("wal_nonexistent");
        await Assert.ThrowsAsync<NotFoundException>(() => bad.Invoices.ListAsync());
    }

    [Fact, TestPriority(1202)]
    public async Task RejectsAccessToWalletOwnedByAnotherUser()
    {
        if (IsSkipped) return;
        var otherAccount = await new LnBotClient().RegisterAsync();
        var otherClient = new LnBotClient(otherAccount.PrimaryKey);
        await Assert.ThrowsAnyAsync<LnBotException>(
            () => otherClient.Wallet(WalletId!).GetAsync());
    }

    // =========================================================================
    // CLEANUP: RETURN FUNDS + DELETE
    // =========================================================================

    [Fact, TestPriority(9000)]
    public async Task Cleanup_ReturnFundsToWallet1()
    {
        if (IsSkipped) return;
        var w2 = s_client!.Wallet(s_w2Info!.WalletId);
        var w2Balance = await w2.GetAsync();

        if (w2Balance.Available > 0)
        {
            var inv = await s_w1!.Invoices.CreateAsync(new CreateInvoiceRequest { Amount = w2Balance.Available });
            var p = await w2.Payments.CreateAsync(new CreatePaymentRequest { Target = inv.Bolt11 });
            var settled = await WaitForPayment(w2, p.Number);
            Assert.Equal(PaymentStatus.Settled, settled.Status);
        }

        var after = await w2.GetAsync();
        Assert.Equal(0, after.Balance);
    }

    [Fact, TestPriority(9001)]
    public async Task Cleanup_Wallet1BalanceIsRestored()
    {
        if (IsSkipped) return;
        var w1After = await s_w1!.GetAsync();
        Assert.True(w1After.Balance >= s_w1BalanceBefore - 10);
    }

    [Fact, TestPriority(9002)]
    public async Task Cleanup_AddressesDelete_RemovesAddress()
    {
        if (IsSkipped) return;
        var w2 = s_client!.Wallet(s_w2Info!.WalletId);
        await w2.Addresses.DeleteAsync(s_w2Address!.Address);
        var addresses = await w2.Addresses.ListAsync();
        Assert.DoesNotContain(addresses, a => a.Address == s_w2Address.Address);
    }

    [Fact, TestPriority(9003)]
    public async Task Cleanup_AddressesDelete_RejectsAlreadyDeleted()
    {
        if (IsSkipped) return;
        var w2 = s_client!.Wallet(s_w2Info!.WalletId);
        await Assert.ThrowsAsync<NotFoundException>(
            () => w2.Addresses.DeleteAsync(s_w2Address!.Address));
    }

    [Fact, TestPriority(9004)]
    public async Task Cleanup_WalletKeyDelete_RevokesKey()
    {
        if (IsSkipped) return;
        await s_client!.Wallet(s_w2Info!.WalletId).Key.DeleteAsync();
        var deadClient = new LnBotClient(s_w2Key!.Key);
        await Assert.ThrowsAnyAsync<LnBotException>(
            () => deadClient.Wallet("current").GetAsync());
    }

    [Fact, TestPriority(9005)]
    public async Task Cleanup_WalletKeyGet_RejectsAfterDelete()
    {
        if (IsSkipped) return;
        await Assert.ThrowsAsync<NotFoundException>(
            () => s_client!.Wallet(s_w2Info!.WalletId).Key.GetAsync());
    }
}

// ── Test ordering infrastructure ──

[AttributeUsage(AttributeTargets.Method)]
public sealed class TestPriorityAttribute : Attribute
{
    public int Priority { get; }
    public TestPriorityAttribute(int priority) => Priority = priority;
}

public sealed class PriorityOrderer : ITestCaseOrderer
{
    public IEnumerable<TTestCase> OrderTestCases<TTestCase>(IEnumerable<TTestCase> testCases) where TTestCase : ITestCase
    {
        return testCases.OrderBy(tc =>
        {
            var attr = tc.TestMethod.Method
                .GetCustomAttributes(typeof(TestPriorityAttribute).AssemblyQualifiedName!)
                .FirstOrDefault();
            return attr?.GetNamedArgument<int>("Priority") ?? 0;
        });
    }
}
