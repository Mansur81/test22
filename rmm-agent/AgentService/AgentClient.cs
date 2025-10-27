using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;

namespace RmmAgent;

public class AgentClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AgentClient> _logger;

    public AgentClient(HttpClient httpClient, ILogger<AgentClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task SendHeartbeatAsync(AgentConfig config, AgentPayload payload, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(new Uri(config.ServerUrl), "/api/agents/heartbeat"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        if (!string.IsNullOrWhiteSpace(config.ApiKey))
        {
            request.Headers.Add("X-API-KEY", config.ApiKey);
        }

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });

        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Server returned {(int)response.StatusCode}: {response.ReasonPhrase}. Content: {content}");
        }

        _logger.LogDebug("Server response: {StatusCode}", response.StatusCode);
    }

    public static IAsyncPolicy<HttpResponseMessage> CreateRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(message => (int)message.StatusCode >= 500)
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }
}
