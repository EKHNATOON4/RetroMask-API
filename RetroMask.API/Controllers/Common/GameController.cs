using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetroMask.Application.Common;
using RetroMask.Application.Dtos.Game;
using RetroMask.Application.Services.Game;

namespace RetroMask.API.Controllers.Common;

/// <summary>
/// Icebreaker games within sessions: browse available games, start/play/complete, and leaderboard.
/// Supports 8 built-in game types (Two Truths, Emoji Story, Desert Island, etc.).
/// </summary>
[Authorize]
[ApiController]
[Route("api/sessions/{sessionId:guid}/game")]
[Produces("application/json")]
public class GameController : ControllerBase
{
    private readonly IGameService _service;

    public GameController(IGameService service)
    {
        _service = service;
    }

    /// <summary>List all available icebreaker game types.</summary>
    /// <param name="sessionId">Session ID (route context).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Returns 8 built-in game types with descriptions.</response>
    [HttpGet("available")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAvailableGames(Guid sessionId, CancellationToken ct)
        => Ok(await _service.GetAvailableGamesAsync(ct));

    /// <summary>Get the currently active icebreaker game for a session.</summary>
    /// <param name="sessionId">Session ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Returns the active game or null.</response>
    [HttpGet("active")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActive(Guid sessionId, CancellationToken ct)
        => Ok(await _service.GetActiveGameAsync(sessionId, ct));

    /// <summary>Start an icebreaker game in a session. Only one game can be active at a time.</summary>
    /// <param name="sessionId">Session ID.</param>
    /// <param name="request">Game type to start.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Game started. Broadcasted to all session participants via SignalR.</response>
    [HttpPost("start")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Start(Guid sessionId, [FromBody] StartGameRequest request, CancellationToken ct)
        => Ok(await _service.StartGameAsync(sessionId, request, ct));

    /// <summary>Submit an answer for the active icebreaker game.</summary>
    /// <param name="sessionId">Session ID (route context).</param>
    /// <param name="gameId">Game ID.</param>
    /// <param name="request">Player's answer text.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Answer recorded with score.</response>
    [HttpPost("{gameId:guid}/answer")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> SubmitAnswer(Guid sessionId, Guid gameId, [FromBody] SubmitAnswerRequest request, CancellationToken ct)
        => Ok(await _service.SubmitAnswerAsync(gameId, request, ct));

    /// <summary>Complete the active icebreaker game and finalize scores.</summary>
    /// <param name="sessionId">Session ID (route context).</param>
    /// <param name="gameId">Game ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Game completed.</response>
    [HttpPost("{gameId:guid}/complete")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Complete(Guid sessionId, Guid gameId, CancellationToken ct)
        => Ok(await _service.CompleteGameAsync(gameId, ct));

    /// <summary>Get the leaderboard (ranked scores) for a completed game.</summary>
    /// <param name="sessionId">Session ID (route context).</param>
    /// <param name="gameId">Game ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Returns ranked list of players with scores.</response>
    [HttpGet("{gameId:guid}/leaderboard")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Leaderboard(Guid sessionId, Guid gameId, CancellationToken ct)
        => Ok(await _service.GetLeaderboardAsync(gameId, ct));
}
