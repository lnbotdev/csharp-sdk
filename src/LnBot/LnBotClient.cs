using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using LnBot.Exceptions;
using LnBot.Models;
using LnBot.Resources;

namespace LnBot;

/// <summary>
/// The official .NET client for the LnBot API.
/// </summary>
public sealed class LnBotClient : ILnBotClient
{
    internal const string Version = "1.0.0";
    internal static readonly string DefaultBaseUrl = "https://api.ln.bot";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    private readonly HttpClient _http;
    private readonly bool _ownsHttpClient;

    /// <summary>Account-level wallet management (create, list).</summary>
    public WalletsResource Wallets { get; }

    /// <summary>Account-level API key management (uk_ keys).</summary>
    public KeysResource Keys { get; }

    /// <summary>Public invoice creation (no auth required).</summary>
    public PublicInvoicesResource Invoices { get; }

    /// <summary>Backup wallet access.</summary>
    public BackupResource Backup { get; }

    /// <summary>Restore wallet access.</summary>
    public RestoreResource Restore { get; }

    /// <summary>
    /// Creates a new LnBot client.
    /// </summary>
    /// <param name="apiKey">API key (uk_ or wk_). Pass null for unauthenticated usage (e.g. public invoice creation).</param>
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
        Keys = new KeysResource(this);
        Invoices = new PublicInvoicesResource(this);
        Backup = new BackupResource(this);
        Restore = new RestoreResource(this);
    }

    /// <summary>
    /// Registers a new account. Returns user keys and recovery passphrase.
    /// </summary>
    public Task<RegisterResponse> RegisterAsync(CancellationToken cancellationToken = default)
        => PostAsync<RegisterResponse>("/v1/register", null, cancellationToken);

    /// <summary>
    /// Returns the authenticated identity.
    /// </summary>
    public Task<MeResponse> MeAsync(CancellationToken cancellationToken = default)
        => GetAsync<MeResponse>("/v1/me", cancellationToken);

    /// <summary>
    /// Returns a wallet-scoped handle for the given wallet ID.
    /// This is a factory method — it does not make an HTTP call.
    /// </summary>
    /// <param name="walletId">The wallet ID (e.g. "wal_abc123").</param>
    public WalletScope Wallet(string walletId)
    {
        ArgumentException.ThrowIfNullOrEmpty(walletId);
        return new WalletScope(this, walletId);
    }

    // ── Internal HTTP helpers ──

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
