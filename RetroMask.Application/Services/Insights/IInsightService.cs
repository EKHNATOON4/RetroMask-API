using RetroMask.Application.Common;
using RetroMask.Application.Dtos.Insights;

namespace RetroMask.Application.Services.Insights;

public interface IInsightService
{
    Task<ApiResponse<UserInsightDto>> GetMyInsightsAsync(CancellationToken ct = default);
    Task<ApiResponse<IEnumerable<GrowthSnapshotDto>>> GetGrowthSnapshotsAsync(int months, CancellationToken ct = default);
    Task<ApiResponse<UserInsightDto>> GetUserInsightsAsync(string userId, CancellationToken ct = default);
    Task RefreshInsightsAsync(string userId, CancellationToken ct = default);
}
