using RetroMask.Application.Common;
using RetroMask.Application.Dtos.Voting;

namespace RetroMask.Application.Services.Voting;

public interface IVotingService
{
    Task<ApiResponse<VoteResultDto>> CastVoteAsync(Guid pointId, CastVoteRequest request, CancellationToken ct = default);
    Task<ApiResponse> RemoveVoteAsync(Guid pointId, CancellationToken ct = default);
    Task<ApiResponse<VoteResultDto>> GetVoteSummaryAsync(Guid pointId, CancellationToken ct = default);
    Task<ApiResponse<IEnumerable<VoteResultDto>>> GetSessionVoteSummariesAsync(Guid sessionId, CancellationToken ct = default);
    Task<ApiResponse> CloseVotingAsync(Guid pointId, CancellationToken ct = default);
}
