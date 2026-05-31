using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetroMask.Application.Common;
using RetroMask.Application.Dtos.Sessions;
using RetroMask.Application.Services.Sessions;

namespace RetroMask.API.Controllers.Sessions;

/// <summary>
/// Retrospective session lifecycle: create, start, pause, complete, join, and leave.
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class SessionsController : ControllerBase
{
    private readonly ISessionService _sessionService;

    public SessionsController(ISessionService sessionService)
    {
        _sessionService = sessionService;
    }

    /// <summary>List sessions for a team (paginated).</summary>
    /// <param name="teamId">Team ID.</param>
    /// <param name="page">Page number (default 1).</param>
    /// <param name="pageSize">Items per page (default 20).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Returns paged list of sessions.</response>
    [HttpGet("team/{teamId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTeamSessions(Guid teamId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await _sessionService.GetTeamSessionsAsync(teamId, page, pageSize, ct));

    /// <summary>Get a session by ID with full details.</summary>
    /// <param name="id">Session ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Returns the session.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<SessionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => Ok(await _sessionService.GetSessionByIdAsync(id, ct));

    /// <summary>Create a new retrospective session with default phases.</summary>
    /// <param name="request">Session title, team ID, and optional settings.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="201">Session created with 5 default phases.</response>
    /// <response code="400">Validation failed or team not found.</response>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<SessionDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateSessionRequest request, CancellationToken ct)
    {
        var result = await _sessionService.CreateSessionAsync(request, ct);
        if (!result.Success || result.Data is null) return BadRequest(result);
        return CreatedAtAction(nameof(GetById), new { id = result.Data.Id }, result);
    }

    /// <summary>Update session settings (Draft sessions only).</summary>
    /// <param name="id">Session ID.</param>
    /// <param name="request">Fields to update.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Session updated.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<SessionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSessionRequest request, CancellationToken ct)
        => Ok(await _sessionService.UpdateSessionAsync(id, request, ct));

    /// <summary>Start a draft session. Activates the first phase and broadcasts via SignalR.</summary>
    /// <param name="id">Session ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Session is now active.</response>
    [HttpPost("{id:guid}/start")]
    [ProducesResponseType(typeof(ApiResponse<SessionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Start(Guid id, CancellationToken ct)
        => Ok(await _sessionService.StartSessionAsync(id, ct));

    /// <summary>Pause an active session.</summary>
    /// <param name="id">Session ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Session paused.</response>
    [HttpPost("{id:guid}/pause")]
    [ProducesResponseType(typeof(ApiResponse<SessionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Pause(Guid id, CancellationToken ct)
        => Ok(await _sessionService.PauseSessionAsync(id, ct));

    /// <summary>Complete a session. Auto-completes any remaining active phases.</summary>
    /// <param name="id">Session ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Session completed.</response>
    [HttpPost("{id:guid}/complete")]
    [ProducesResponseType(typeof(ApiResponse<SessionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Complete(Guid id, CancellationToken ct)
        => Ok(await _sessionService.CompleteSessionAsync(id, ct));

    /// <summary>Join a session. For anonymous sessions, assigns a random mask name.</summary>
    /// <param name="id">Session ID.</param>
    /// <param name="request">Optional join parameters.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Joined successfully.</response>
    [HttpPost("{id:guid}/join")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Join(Guid id, [FromBody] JoinSessionRequest? request, CancellationToken ct)
        => Ok(await _sessionService.JoinSessionAsync(id, request ?? new JoinSessionRequest(), ct));

    /// <summary>Leave a session.</summary>
    /// <param name="id">Session ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Left the session.</response>
    [HttpPost("{id:guid}/leave")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Leave(Guid id, CancellationToken ct)
        => Ok(await _sessionService.LeaveSessionAsync(id, ct));

    /// <summary>Soft-delete a session (facilitator only).</summary>
    /// <param name="id">Session ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Session deleted.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        => Ok(await _sessionService.DeleteSessionAsync(id, ct));
}
