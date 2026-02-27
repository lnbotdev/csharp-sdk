using System.Text.Json.Serialization;

namespace LnBot.Models;

public sealed class InvoiceEvent
{
    [JsonPropertyName("event")]
    public required string Event { get; init; }

    [JsonPropertyName("data")]
    public required InvoiceResponse Data { get; init; }
}

public sealed class PaymentEvent
{
    [JsonPropertyName("event")]
    public required string Event { get; init; }

    [JsonPropertyName("data")]
    public required PaymentResponse Data { get; init; }
}

public sealed class WalletEvent
{
    [JsonPropertyName("event")]
    public required string Event { get; init; }

    [JsonPropertyName("createdAt")]
    public required DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("data")]
    public required System.Text.Json.JsonElement Data { get; init; }
}
