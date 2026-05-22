using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetroMask.Application.Dtos.Game;
using RetroMask.Application.Services.Game;

namespace RetroMask.API.Controllers.Common;

[Authorize]
[ApiController]
[Route("api/sessions/{sessionId:guid}/game")]
public class GameController : ControllerBase
{
    private readonly IGameService _service;

    public GameController(IGameService service)
    {
        _service = service;
    }

    [HttpGet("available")]
    public async Task<IActionResult> GetAvailableGames(Guid sessionId, CancellationToken ct)
        => Ok(await _service.GetAvailableGamesAsync(ct));

    [HttpGet("active")]
    public async Task<IActionResult> GetActive(Guid sessionId, CancellationToken ct)
        => Ok(await _service.GetActiveGameAsync(sessionId, ct));

    [HttpPost("start")]
    public async Task<IActionResult> Start(Guid sessionId, [FromBody] StartGameRequest request, CancellationToken ct)
        => Ok(await _service.StartGameAsync(sessionId, request, ct));

    [HttpPost("{gameId:guid}/answer")]
    public async Task<IActionResult> SubmitAnswer(Guid sessionId, Guid gameId, [FromBody] SubmitAnswerRequest request, CancellationToken ct)
        => Ok(await _service.SubmitAnswerAsync(gameId, request, ct));

    [HttpPost("{gameId:guid}/complete")]
    public async Task<IActionResult> Complete(Guid sessionId, Guid gameId, CancellationToken ct)
        => Ok(await _service.CompleteGameAsync(gameId, ct));

    [HttpGet("{gameId:guid}/leaderboard")]
    public async Task<IActionResult> Leaderboard(Guid sessionId, Guid gameId, CancellationToken ct)
        => Ok(await _service.GetLeaderboardAsync(gameId, ct));
}
