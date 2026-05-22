using RetroMask.Application.Common;
using RetroMask.Application.Dtos.AI;

namespace RetroMask.Application.Services.AI;

public interface IAIService
{
    Task<ApiResponse<AIInsightDto>> GenerateSummaryAsync(Guid sessionId, CancellationToken ct = default);
    Task<ApiResponse<IEnumerable<AIClusterDto>>> ClusterPointsAsync(Guid sessionId, CancellationToken ct = default);
    Task<ApiResponse<AIInsightDto>> AnalyzeSentimentAsync(Guid sessionId, CancellationToken ct = default);
    Task<ApiResponse<AIReportDto>> GenerateReportAsync(Guid sessionId, CancellationToken ct = default);
    Task<ApiResponse<AIInsightDto>> GenerateRecommendationsAsync(Guid sessionId, CancellationToken ct = default);
    Task<ApiResponse<IEnumerable<AIInsightDto>>> GetSessionInsightsAsync(Guid sessionId, CancellationToken ct = default);
}
