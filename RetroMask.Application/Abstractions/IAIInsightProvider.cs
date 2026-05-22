namespace RetroMask.Application.Abstractions;

public interface IAIInsightProvider
{
    Task<string> GenerateSummaryAsync(string prompt, CancellationToken ct = default);
    Task<string> ClusterPointsAsync(IEnumerable<string> points, CancellationToken ct = default);
    Task<string> AnalyzeSentimentAsync(string text, CancellationToken ct = default);
    Task<string> GenerateReportAsync(string prompt, CancellationToken ct = default);
    Task<string> GenerateRecommendationsAsync(string context, CancellationToken ct = default);
}
