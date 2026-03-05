namespace LnBot.Resources;

using LnBot.Models;

/// <summary>Wallet-scoped L402 paywall authentication.</summary>
public sealed class L402Resource
{
    private readonly LnBotClient _client;
    private readonly string _prefix;

    internal L402Resource(LnBotClient client, string prefix)
    {
        _client = client;
        _prefix = prefix;
    }

    /// <summary>Creates an L402 challenge (invoice + macaroon) for paywall authentication.</summary>
    public Task<L402ChallengeResponse> CreateChallengeAsync(CreateL402ChallengeRequest request, CancellationToken cancellationToken = default)
        => _client.PostAsync<L402ChallengeResponse>($"{_prefix}/l402/challenges", request, cancellationToken);

    /// <summary>Verifies an L402 authorization token (stateless).</summary>
    public Task<VerifyL402Response> VerifyAsync(VerifyL402Request request, CancellationToken cancellationToken = default)
        => _client.PostAsync<VerifyL402Response>($"{_prefix}/l402/verify", request, cancellationToken);

    /// <summary>Pays an L402 challenge and returns a ready-to-use Authorization header.</summary>
    public Task<L402PayResponse> PayAsync(PayL402Request request, CancellationToken cancellationToken = default)
        => _client.PostAsync<L402PayResponse>($"{_prefix}/l402/pay", request, cancellationToken);
}
