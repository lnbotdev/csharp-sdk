using LnBot.Models;

namespace LnBot.Resources;

/// <summary>
/// Wallet-scoped transaction history.
/// </summary>
public sealed class TransactionsResource
{
    private readonly LnBotClient _client;
    private readonly string _prefix;

    internal TransactionsResource(LnBotClient client, string prefix)
    {
        _client = client;
        _prefix = prefix;
    }

    /// <summary>
    /// Lists transactions in reverse chronological order.
    /// </summary>
    public Task<List<TransactionResponse>> ListAsync(PaginationParams? pagination = null, CancellationToken cancellationToken = default)
    {
        var path = $"{_prefix}/transactions";
        if (pagination is not null)
        {
            var parts = new List<string>();
            if (pagination.Limit.HasValue) parts.Add($"limit={pagination.Limit.Value}");
            if (pagination.After.HasValue) parts.Add($"after={pagination.After.Value}");
            if (parts.Count > 0) path += $"?{string.Join('&', parts)}";
        }
        return _client.GetAsync<List<TransactionResponse>>(path, cancellationToken);
    }
}
