using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace SkyV.Launcher;

public sealed class QueueApiClient
{
    private readonly HttpClient http;

    public QueueApiClient(string baseUrl)
    {
        http = new HttpClient
        {
            BaseAddress = new Uri(baseUrl, UriKind.Absolute),
            Timeout = TimeSpan.FromSeconds(20),
        };
    }

    public async Task<EnqueueResponse> EnqueueAsync(string ticket, string serverId, CancellationToken cancellationToken)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, "/v1/enqueue");
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", ticket);
        req.Content = JsonContent.Create(new EnqueueRequest { ServerId = serverId });
        var resp = await http.SendAsync(req, cancellationToken).ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode)
        {
            var err = await resp.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken: cancellationToken).ConfigureAwait(false);
            throw new QueueApiException(resp.StatusCode, err?.Error ?? "Unknown queue error");
        }
        var parsed = await resp.Content.ReadFromJsonAsync<EnqueueResponse>(cancellationToken: cancellationToken).ConfigureAwait(false);
        return parsed ?? throw new InvalidOperationException("Empty response from queue enqueue.");
    }

    public async Task<StatusResponse> StatusAsync(string ticket, string queueId, CancellationToken cancellationToken)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, $"/v1/status?queue_id={Uri.EscapeDataString(queueId)}");
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", ticket);
        var resp = await http.SendAsync(req, cancellationToken).ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode)
        {
            var err = await resp.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken: cancellationToken).ConfigureAwait(false);
            throw new QueueApiException(resp.StatusCode, err?.Error ?? "Unknown queue error");
        }
        var parsed = await resp.Content.ReadFromJsonAsync<StatusResponse>(cancellationToken: cancellationToken).ConfigureAwait(false);
        return parsed ?? throw new InvalidOperationException("Empty response from queue status.");
    }

    public async Task CancelAsync(string ticket, string queueId, CancellationToken cancellationToken)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, "/v1/cancel");
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", ticket);
        req.Content = JsonContent.Create(new CancelRequest { QueueId = queueId });
        var resp = await http.SendAsync(req, cancellationToken).ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode)
        {
            var err = await resp.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken: cancellationToken).ConfigureAwait(false);
            throw new QueueApiException(resp.StatusCode, err?.Error ?? "Unknown queue error");
        }
    }

    public sealed class EnqueueRequest
    {
        [JsonPropertyName("server_id")]
        public string ServerId { get; set; } = "";
    }

    public sealed class EnqueueResponse
    {
        [JsonPropertyName("queue_id")]
        public string QueueId { get; set; } = "";

        [JsonPropertyName("position")]
        public int Position { get; set; }

        [JsonPropertyName("state")]
        public string State { get; set; } = "";

        [JsonPropertyName("admit_expires_at_unix_ms")]
        public long AdmitExpiresAtUnixMs { get; set; }
    }

    public sealed class StatusResponse
    {
        [JsonPropertyName("queue_id")]
        public string QueueId { get; set; } = "";

        [JsonPropertyName("position")]
        public int Position { get; set; }

        [JsonPropertyName("state")]
        public string State { get; set; } = "";

        [JsonPropertyName("admit_expires_at_unix_ms")]
        public long AdmitExpiresAtUnixMs { get; set; }
    }

    public sealed class CancelRequest
    {
        [JsonPropertyName("queue_id")]
        public string QueueId { get; set; } = "";
    }

    public sealed class ErrorResponse
    {
        [JsonPropertyName("error")]
        public string Error { get; set; } = "";
    }
}

public sealed class QueueApiException : Exception
{
    public HttpStatusCode StatusCode { get; }

    public QueueApiException(HttpStatusCode statusCode, string message) : base(message)
    {
        StatusCode = statusCode;
    }
}
