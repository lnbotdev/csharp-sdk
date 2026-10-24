using System.Text.Json.Serialization;

namespace LnBot.Models;

public sealed class InvoiceEvent
{
    /// <summary>
    /// <c>settled</c> or <c>expired</c>, or <c>timeout</c> when the wait ran out:
    /// <see cref="Data"/> is then the current state; read it again or watch again.
    /// </summary>
    [JsonPropertyName("event")]
    public required string Event { get; init; }

    [JsonPropertyName("data")]
    public required InvoiceResponse Data { get; init; }
}

public sealed class PaymentEvent
{
    /// <summary>
    /// <c>settled</c> or <c>failed</c>, or <c>timeout</c> when the wait ran out:
    /// <see cref="Data"/> is then the current state; read it again or watch again.
    /// </summary>
    [JsonPropertyName("event")]
    public required string Event { get; init; }

    [JsonPropertyName("data")]
    public required PaymentResponse Data { get; init; }
}

public sealed class WalletEvent
{
    /// <summary>
    /// <c>invoice.created</c>, <c>invoice.settled</c>, <c>payment.created</c>, <c>payment.settled</c> or <c>payment.failed</c>.
    /// </summary>
    [JsonPropertyName("event")]
    public required string Event { get; init; }

    [JsonPropertyName("createdAt")]
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// The invoice or payment, with the same camelCase keys as REST responses, so it decodes into
    /// <see cref="InvoiceResponse"/> or <see cref="PaymentResponse"/>. The API sends it in snake_case and the SDK converts it.
    /// <c>*.created</c> events carry only the fields known at creation; the others decode to null.
    /// </summary>
    [JsonPropertyName("data")]
    public required System.Text.Json.JsonElement Data { get; init; }
}
