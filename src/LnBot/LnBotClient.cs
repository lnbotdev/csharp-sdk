using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using LnBot.Exceptions;
using LnBot.Resources;

namespace LnBot;

/// <summary>
/// The official .NET client for the LnBot API.
/// </summary>
public sealed class LnBotClient : IDisposable
{
    internal const string Version = "0.4.0";
    internal static readonly string DefaultBaseUrl = "https://api.ln.bot";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    private readonly HttpClient _http;
    private readonly bool _ownsHttpClient;

    /// <summary>Wallet management.</summary>
    public WalletsResource Wallets { get; }

    /// <summary>Create and query invoices.</summary>
    public InvoicesResource Invoices { get; }

    /// <summary>Send payments.</summary>
    public PaymentsResource Payments { get; }

    /// <summary>Lightning address management.</summary>
    public AddressesResource Addresses { get; }

    /// <summary>Transaction history.</summary>
    public TransactionsResource Transactions { get; }

    /// <summary>Webhook endpoints.</summary>
    public WebhooksResource Webhooks { get; }

    /// <summary>API key management.</summary>
    public KeysResource Keys { get; }

    /// <summary>Real-time event stream.</summary>
    public EventsResource Events { get; }

    /// <summary>Backup wallet access.</summary>
    public BackupResource Backup { get; }

    /// <summary>Restore wallet access.</summary>
    public RestoreResource Restore { get; }

    /// <summary>
    /// Creates a new LnBot client.
    /// </summary>
    /// <param name="apiKey">API key for authenticated endpoints. Pass null or empty for unauthenticated usage (e.g. wallet creation).</param>
    /// <param name="options">Optional configuration.</param>
    public LnBotClient(string? apiKey = null, LnBotClientOptions? options = null)
    {
        options ??= new LnBotClientOptions();

        if (options.HttpClient is not null)
        {
            _http = options.HttpClient;
            _ownsHttpClient = false;
        }
        else
        {
            _http = new HttpClient();
            _ownsHttpClient = true;
        }

        var baseUrl = (options.BaseUrl ?? DefaultBaseUrl).TrimEnd('/');
        _http.BaseAddress = new Uri(baseUrl);
        _http.DefaultRequestHeaders.UserAgent.ParseAdd($"lnbot-csharp/{Version}");

        if (!string.IsNullOrEmpty(apiKey))
        {
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        }

        if (options.Timeout.HasValue)
        {
            _http.Timeout = options.Timeout.Value;
        }

        Wallets = new WalletsResource(this);
        Invoices = new InvoicesResource(this);
        Payments = new PaymentsResource(this);
        Addresses = new AddressesResource(this);
        Transactions = new TransactionsResource(this);
        Webhooks = new WebhooksResource(this);
        Keys = new KeysResource(this);
        Events = new EventsResource(this);
        Backup = new BackupResource(this);
        Restore = new RestoreResource(this);
    }

    internal async Task<T> GetAsync<T>(string path, CancellationToken cancellationToken = default)
    {
        using var response = await _http.GetAsync(path, cancellationToken).ConfigureAwait(false);
        return await HandleResponseAsync<T>(response, cancellationToken).ConfigureAwait(false);
    }

    internal async Task<T> PostAsync<T>(string path, object? body = null, CancellationToken cancellationToken = default)
    {
        using var content = SerializeBody(body);
        using var response = await _http.PostAsync(path, content, cancellationToken).ConfigureAwait(false);
        return await HandleResponseAsync<T>(response, cancellationToken).ConfigureAwait(false);
    }

    internal async Task PostAsync(string path, object? body = null, CancellationToken cancellationToken = default)
    {
        using var content = SerializeBody(body);
        using var response = await _http.PostAsync(path, content, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
    }

    internal async Task<T> PatchAsync<T>(string path, object body, CancellationToken cancellationToken = default)
    {
        using var content = SerializeBody(body);
        using var request = new HttpRequestMessage(HttpMethod.Patch, path) { Content = content };
        using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        return await HandleResponseAsync<T>(response, cancellationToken).ConfigureAwait(false);
    }

    internal async Task DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        using var response = await _http.DeleteAsync(path, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
    }

    internal async Task<Stream> GetStreamAsync(string path, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
        return await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
    }

    internal static JsonSerializerOptions GetJsonOptions() => JsonOptions;

    private static StringContent? SerializeBody(object? body)
    {
        if (body is null) return null;
        var json = JsonSerializer.Serialize(body, JsonOptions);
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    private static async Task<T> HandleResponseAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            await ThrowApiExceptionAsync(response, cancellationToken).ConfigureAwait(false);
        }

        return (await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken).ConfigureAwait(false))!;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            await ThrowApiExceptionAsync(response, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task ThrowApiExceptionAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var message = ParseErrorMessage(body) ?? response.ReasonPhrase ?? "Unknown error";
        var statusCode = (int)response.StatusCode;

        throw statusCode switch
        {
            400 => new BadRequestException(message, body),
            401 => new UnauthorizedException(message, body),
            403 => new ForbiddenException(message, body),
            404 => new NotFoundException(message, body),
            409 => new ConflictException(message, body),
            _ => new LnBotException(statusCode, message, body),
        };
    }

    private static string? ParseErrorMessage(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("message", out var msg) && msg.ValueKind == JsonValueKind.String)
                return msg.GetString();
            if (doc.RootElement.TryGetProperty("error", out var err) && err.ValueKind == JsonValueKind.String)
                return err.GetString();
        }
        catch (JsonException) { }
        return null;
    }

    public void Dispose()
    {
        if (_ownsHttpClient) _http.Dispose();
    }
}

/// <summary>
/// Configuration options for <see cref="LnBotClient"/>.
/// </summary>
public sealed class LnBotClientOptions
{
    /// <summary>Base URL for the API. Defaults to https://api.ln.bot.</summary>
    public string? BaseUrl { get; set; }

    /// <summary>Custom HttpClient instance. The client will not be disposed by LnBotClient.</summary>
    public HttpClient? HttpClient { get; set; }

    /// <summary>Request timeout. Defaults to HttpClient's default (100 seconds).</summary>
    public TimeSpan? Timeout { get; set; }
}
