using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace LnBot;

/// <summary>
/// A message read from a Server-Sent Events stream: its event name and its data.
/// </summary>
internal readonly record struct ServerSentEvent(string Event, string Data);

/// <summary>
/// Reads and decodes the API's Server-Sent Events streams.
/// Event payloads are snake_case on the wire (<c>actual_fee</c>, <c>created_at</c>), unlike REST responses,
/// so their keys are converted to camelCase before they are decoded into the SDK's models.
/// </summary>
internal static class ServerSentEvents
{
    /// <summary>
    /// Reads messages from an SSE stream. Comment lines (such as <c>:keepalive</c>) are skipped,
    /// multi-line data is joined with <c>\n</c>, and a message without an <c>event:</c> field is named <c>message</c>.
    /// </summary>
    internal static async IAsyncEnumerable<ServerSentEvent> ReadAsync(Stream stream, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(stream);
        var eventName = "";
        var data = new StringBuilder();
        var hasData = false;

        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line is null) break;

            if (line.Length == 0)
            {
                if (hasData) yield return new ServerSentEvent(eventName.Length > 0 ? eventName : "message", data.ToString());
                eventName = "";
                data.Clear();
                hasData = false;
                continue;
            }

            if (line[0] == ':') continue;

            var colon = line.IndexOf(':');
            var field = colon < 0 ? line : line[..colon];
            var value = colon < 0 ? "" : line[(colon + 1)..];
            if (value.StartsWith(' ')) value = value[1..];

            switch (field)
            {
                case "event":
                    eventName = value;
                    break;
                case "data":
                    if (hasData) data.Append('\n');
                    data.Append(value);
                    hasData = true;
                    break;
            }
        }
    }

    /// <summary>
    /// Decodes a message's data as <typeparamref name="T"/>. Throws <see cref="JsonException"/> if it can't be decoded.
    /// </summary>
    internal static T Deserialize<T>(ServerSentEvent message) where T : class
        => Deserialize<T>(ParseCamelCase(message.Data), message.Event);

    /// <summary>
    /// Decodes a message from a single invoice or payment watch stream. The event name comes from the
    /// <c>event:</c> field and the data is the object itself. The <c>{"event":...,"data":{...}}</c> envelope
    /// that earlier SDK versions expected is still accepted.
    /// Throws <see cref="JsonException"/> if the data can't be decoded.
    /// </summary>
    internal static (string Event, T Data) DeserializeWatchEvent<T>(ServerSentEvent message) where T : class
    {
        var root = ParseCamelCase(message.Data);
        if (root.ValueKind == JsonValueKind.Object
            && root.TryGetProperty("event", out var name) && name.ValueKind == JsonValueKind.String
            && root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object)
        {
            var eventName = name.GetString()!;
            return (eventName, Deserialize<T>(data, eventName));
        }

        return (message.Event, Deserialize<T>(root, message.Event));
    }

    /// <summary>
    /// Like <see cref="Deserialize{T}(ServerSentEvent)"/>, but returns false instead of throwing, so a stream
    /// can skip a message it can't decode, as it always has.
    /// </summary>
    internal static bool TryDeserialize<T>(ServerSentEvent message, [NotNullWhen(true)] out T? value) where T : class
    {
        try
        {
            value = Deserialize<T>(message);
            return true;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }

    /// <summary>
    /// Like <see cref="DeserializeWatchEvent{T}(ServerSentEvent)"/>, but returns false instead of throwing.
    /// </summary>
    internal static bool TryDeserializeWatchEvent<T>(ServerSentEvent message, out string name, [NotNullWhen(true)] out T? data) where T : class
    {
        try
        {
            (name, data) = DeserializeWatchEvent<T>(message);
            return true;
        }
        catch (JsonException)
        {
            name = message.Event;
            data = null;
            return false;
        }
    }

    /// <summary>
    /// Converts an object key from snake_case to camelCase (<c>actual_fee</c> to <c>actualFee</c>).
    /// Keys without an underscore, including keys already in camelCase, are returned unchanged.
    /// </summary>
    internal static string ToCamelCase(string key)
    {
        if (key.IndexOf('_') < 0) return key;

        var builder = new StringBuilder(key.Length);
        var upper = false;
        for (var i = 0; i < key.Length; i++)
        {
            var c = key[i];
            if (c == '_' && i > 0 && i < key.Length - 1)
            {
                upper = true;
                continue;
            }
            builder.Append(upper ? char.ToUpperInvariant(c) : c);
            upper = false;
        }
        return builder.ToString();
    }

    private static T Deserialize<T>(JsonElement element, string eventName) where T : class
        => element.Deserialize<T>(LnBotClient.GetJsonOptions())
            ?? throw new JsonException($"The '{eventName}' event has no data.");

    /// <summary>
    /// Parses JSON and converts the keys of every object in it, at any depth, to camelCase.
    /// </summary>
    private static JsonElement ParseCamelCase(string json)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var document = JsonDocument.Parse(json))
        using (var writer = new Utf8JsonWriter(buffer))
        {
            WriteCamelCase(document.RootElement, writer);
        }
        return JsonSerializer.Deserialize<JsonElement>(buffer.WrittenSpan);
    }

    private static void WriteCamelCase(JsonElement element, Utf8JsonWriter writer)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in element.EnumerateObject())
                {
                    writer.WritePropertyName(ToCamelCase(property.Name));
                    WriteCamelCase(property.Value, writer);
                }
                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                    WriteCamelCase(item, writer);
                writer.WriteEndArray();
                break;
            default:
                element.WriteTo(writer);
                break;
        }
    }
}
