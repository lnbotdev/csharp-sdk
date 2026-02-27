using System.Net;
using System.Text;
using System.Text.Json;

namespace LnBot.Tests;

/// <summary>
/// Mock HTTP handler that captures requests and returns configured responses.
/// Inject via LnBotClientOptions.HttpClient.
/// </summary>
internal class MockHandler : HttpMessageHandler
{
    private HttpStatusCode _statusCode = HttpStatusCode.OK;
    private string _responseBody = "{}";
    private string _contentType = "application/json";

    public HttpRequestMessage? LastRequest { get; private set; }
    public string? LastRequestBody { get; private set; }

    public void SetResponse(object body, HttpStatusCode status = HttpStatusCode.OK)
    {
        _statusCode = status;
        _responseBody = JsonSerializer.Serialize(body, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        });
        _contentType = "application/json";
    }

    public void SetRawResponse(string body, HttpStatusCode status = HttpStatusCode.OK, string contentType = "application/json")
    {
        _statusCode = status;
        _responseBody = body;
        _contentType = contentType;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        LastRequest = request;
        LastRequestBody = request.Content is not null
            ? await request.Content.ReadAsStringAsync(cancellationToken)
            : null;

        return new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_responseBody, Encoding.UTF8, _contentType),
        };
    }
}

/// <summary>
/// Mock HTTP handler that returns an SSE stream.
/// </summary>
internal class MockSseHandler : HttpMessageHandler
{
    private readonly string _sseContent;
    private readonly HttpStatusCode _statusCode;

    public HttpRequestMessage? LastRequest { get; private set; }

    public MockSseHandler(string sseContent, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _sseContent = sseContent;
        _statusCode = statusCode;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        LastRequest = request;

        if (_statusCode != HttpStatusCode.OK)
        {
            return Task.FromResult(new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent("error", Encoding.UTF8, "text/plain"),
            });
        }

        var stream = new MemoryStream(Encoding.UTF8.GetBytes(_sseContent));
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StreamContent(stream)
            {
                Headers = { { "Content-Type", "text/event-stream" } },
            },
        });
    }
}

/// <summary>
/// Helper to create LnBotClient with a mock handler.
/// </summary>
internal static class TestHelper
{
    public static (LnBotClient Client, MockHandler Handler) CreateClient(string apiKey = "key_test")
    {
        var handler = new MockHandler();
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.ln.bot") };
        var client = new LnBotClient(apiKey, new LnBotClientOptions { HttpClient = http });
        return (client, handler);
    }

    public static (LnBotClient Client, MockSseHandler Handler) CreateSseClient(string sseContent, string apiKey = "key_test")
    {
        var handler = new MockSseHandler(sseContent);
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.ln.bot") };
        var client = new LnBotClient(apiKey, new LnBotClientOptions { HttpClient = http });
        return (client, handler);
    }
}
