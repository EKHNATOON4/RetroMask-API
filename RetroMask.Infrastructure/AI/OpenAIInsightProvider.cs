using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using RetroMask.Application.Abstractions;

namespace RetroMask.Infrastructure.AI;

public class OpenAIInsightProvider : IAIInsightProvider
{
    private readonly ChatClient _chatClient;
    private readonly ILogger<OpenAIInsightProvider> _logger;
    private readonly string _model;

    public OpenAIInsightProvider(IConfiguration configuration, ILogger<OpenAIInsightProvider> logger)
    {
        _logger = logger;
        _model = configuration["OpenAI:Model"] ?? "gpt-4o-mini";
        var apiKey = configuration["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI ApiKey not configured.");
        _chatClient = new ChatClient(_model, apiKey);
    }

    public async Task<string> GenerateSummaryAsync(string prompt, CancellationToken ct = default)
        => await CallOpenAIAsync(prompt, ct);

    public async Task<string> ClusterPointsAsync(IEnumerable<string> points, CancellationToken ct = default)
        => await CallOpenAIAsync(PromptTemplates.ClusterPoints(points), ct);

    public async Task<string> AnalyzeSentimentAsync(string text, CancellationToken ct = default)
        => await CallOpenAIAsync(PromptTemplates.SentimentAnalysis(text), ct);

    public async Task<string> GenerateReportAsync(string prompt, CancellationToken ct = default)
        => await CallOpenAIAsync(prompt, ct);

    public async Task<string> GenerateRecommendationsAsync(string context, CancellationToken ct = default)
        => await CallOpenAIAsync(PromptTemplates.Recommendations(context), ct);

    private async Task<string> CallOpenAIAsync(string prompt, CancellationToken ct)
    {
        _logger.LogDebug("Calling OpenAI model {Model}", _model);
        var response = await _chatClient.CompleteChatAsync(
            new[] { new UserChatMessage(prompt) }, cancellationToken: ct);
        return response.Value.Content[0].Text;
    }
}
