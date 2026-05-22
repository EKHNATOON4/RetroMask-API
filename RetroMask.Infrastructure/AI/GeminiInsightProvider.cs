using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RetroMask.Application.Abstractions;
using System.Net.Http.Json;
using System.Text.Json;

namespace RetroMask.Infrastructure.AI;

/// <summary>
/// Gemini AI provider using the REST API directly (no official .NET SDK yet).
/// </summary>
public class GeminiInsightProvider : IAIInsightProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GeminiInsightProvider> _logger;
    private readonly string _apiKey;
    private readonly string _model;
    private const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta/models";

    public GeminiInsightProvider(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<GeminiInsightProvider> logger)
    {
        _httpClient = httpClientFactory.CreateClient("Gemini");
        _logger = logger;
        _apiKey = configuration["Gemini:ApiKey"] ?? throw new InvalidOperationException("Gemini ApiKey not configured.");
        _model = configuration["Gemini:Model"] ?? "gemini-1.5-flash";
    }

    public async Task<string> GenerateSummaryAsync(string prompt, CancellationToken ct = default) => await CallGeminiAsync(prompt, ct);
    public async Task<string> ClusterPointsAsync(IEnumerable<string> points, CancellationToken ct = default) => await CallGeminiAsync(PromptTemplates.ClusterPoints(points), ct);
    public async Task<string> AnalyzeSentimentAsync(string text, CancellationToken ct = default) => await CallGeminiAsync(PromptTemplates.SentimentAnalysis(text), ct);
    public async Task<string> GenerateReportAsync(string prompt, CancellationToken ct = default) => await CallGeminiAsync(prompt, ct);
    public async Task<string> GenerateRecommendationsAsync(string context, CancellationToken ct = default) => await CallGeminiAsync(PromptTemplates.Recommendations(context), ct);

    private async Task<string> CallGeminiAsync(string prompt, CancellationToken ct)
    {
        _logger.LogDebug("Calling Gemini model {Model}", _model);
        var url = $"{BaseUrl}/{_model}:generateContent?key={_apiKey}";
        var payload = new { contents = new[] { new { parts = new[] { new { text = prompt } } } } };
        var response = await _httpClient.PostAsJsonAsync(url, payload, ct);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString() ?? string.Empty;
    }
}
