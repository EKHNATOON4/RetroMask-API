using RetroMask.Application.Common;
using RetroMask.Application.Dtos.Game;

namespace RetroMask.Application.Services.Game;

public interface IGameService
{
    Task<ApiResponse<GameDto>> StartGameAsync(Guid sessionId, StartGameRequest request, CancellationToken ct = default);
    Task<ApiResponse<GameDto>> GetActiveGameAsync(Guid sessionId, CancellationToken ct = default);
    Task<ApiResponse> SubmitAnswerAsync(Guid gameId, SubmitAnswerRequest request, CancellationToken ct = default);
    Task<ApiResponse<GameDto>> CompleteGameAsync(Guid gameId, CancellationToken ct = default);
    Task<ApiResponse<IEnumerable<GameResultDto>>> GetLeaderboardAsync(Guid gameId, CancellationToken ct = default);
    Task<ApiResponse<IEnumerable<string>>> GetAvailableGamesAsync(CancellationToken ct = default);
}
