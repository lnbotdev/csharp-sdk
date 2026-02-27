namespace LnBot.Resources;

using LnBot.Models;

/// <summary>L402 paywall authentication.</summary>
public sealed class L402Resource
{
    private readonly LnBotClient _client;
    internal L402Resource(LnBotClient client) => _client = client;

    /// <summary>Creates an L402 challenge (invoice + macaroon) for paywall authentication.</summary>
    public Task<L402ChallengeResponse> CreateChallengeAsync(CreateL402ChallengeRequest request, CancellationToken cancellationToken = default)
        => _client.PostAsync<L402ChallengeResponse>("/v1/l402/challenges", request, cancellationToken);

    /// <summary>Verifies an L402 authorization token (stateless).</summary>
    public Task<VerifyL402Response> VerifyAsync(VerifyL402Request request, CancellationToken cancellationToken = default)
        => _client.PostAsync<VerifyL402Response>("/v1/l402/verify", request, cancellationToken);

    /// <summary>Pays an L402 challenge and returns a ready-to-use Authorization header.</summary>
    public Task<L402PayResponse> PayAsync(PayL402Request request, CancellationToken cancellationToken = default)
        => _client.PostAsync<L402PayResponse>("/v1/l402/pay", request, cancellationToken);
}
