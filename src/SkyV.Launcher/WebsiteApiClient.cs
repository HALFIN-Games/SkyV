using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace SkyV.Launcher;

public sealed class WebsiteApiClient
{
    private readonly HttpClient http;

    public WebsiteApiClient(string baseUrl)
    {
        http = new HttpClient
        {
            BaseAddress = new Uri(baseUrl, UriKind.Absolute),
            Timeout = TimeSpan.FromSeconds(20),
        };
    }

    public async Task<ExchangeCodeResponse> ExchangeCodeAsync(string serverId, string code, CancellationToken cancellationToken)
    {
        var req = new ExchangeCodeRequest { ServerId = serverId, Code = code };
        var resp = await http.PostAsJsonAsync("/api/game/exchange-code", req, cancellationToken).ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode)
        {
            var err = await resp.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken: cancellationToken).ConfigureAwait(false);
            throw new WebsiteApiException(resp.StatusCode, err?.Error ?? "Unknown website error");
        }

        var parsed = await resp.Content.ReadFromJsonAsync<ExchangeCodeResponse>(cancellationToken: cancellationToken).ConfigureAwait(false);
        return parsed ?? throw new InvalidOperationException("Empty response from exchange-code endpoint.");
    }

    public sealed class ExchangeCodeRequest
    {
        [JsonPropertyName("server_id")]
        public string ServerId { get; set; } = "";

        [JsonPropertyName("code")]
        public string Code { get; set; } = "";
    }

    public sealed class ExchangeCodeResponse
    {
        [JsonPropertyName("ticket")]
        public string Ticket { get; set; } = "";

        [JsonPropertyName("expires_in_seconds")]
        public int ExpiresInSeconds { get; set; }
    }

    public sealed class ErrorResponse
    {
        [JsonPropertyName("error")]
        public string Error { get; set; } = "";
    }
}

public sealed class WebsiteApiException : Exception
{
    public HttpStatusCode StatusCode { get; }

    public WebsiteApiException(HttpStatusCode statusCode, string message) : base(message)
    {
        StatusCode = statusCode;
    }
}
