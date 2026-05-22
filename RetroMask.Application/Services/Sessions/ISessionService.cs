using RetroMask.Application.Common;
using RetroMask.Application.Dtos.Sessions;

namespace RetroMask.Application.Services.Sessions;

public interface ISessionService
{
    Task<ApiResponse<SessionDto>> CreateSessionAsync(CreateSessionRequest request, CancellationToken ct = default);
    Task<ApiResponse<SessionDto>> GetSessionByIdAsync(Guid sessionId, CancellationToken ct = default);
    Task<ApiResponse<PagedResult<SessionSummaryDto>>> GetTeamSessionsAsync(Guid teamId, int page, int pageSize, CancellationToken ct = default);
    Task<ApiResponse<SessionDto>> StartSessionAsync(Guid sessionId, CancellationToken ct = default);
    Task<ApiResponse<SessionDto>> PauseSessionAsync(Guid sessionId, CancellationToken ct = default);
    Task<ApiResponse<SessionDto>> CompleteSessionAsync(Guid sessionId, CancellationToken ct = default);
    Task<ApiResponse> DeleteSessionAsync(Guid sessionId, CancellationToken ct = default);
    Task<ApiResponse<SessionDto>> UpdateSessionAsync(Guid sessionId, UpdateSessionRequest request, CancellationToken ct = default);
    Task<ApiResponse> JoinSessionAsync(Guid sessionId, JoinSessionRequest request, CancellationToken ct = default);
    Task<ApiResponse> LeaveSessionAsync(Guid sessionId, CancellationToken ct = default);
}
