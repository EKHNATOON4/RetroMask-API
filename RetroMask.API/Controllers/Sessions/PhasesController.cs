using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetroMask.Application.Common;
using RetroMask.Application.Dtos.Sessions;
using RetroMask.Application.Services.Phases;

namespace RetroMask.API.Controllers.Sessions;

/// <summary>
/// Session phase management: activate, complete, skip, extend, and reorder phases.
/// </summary>
[Authorize]
[ApiController]
[Route("api/sessions/{sessionId:guid}/phases")]
[Produces("application/json")]
public class PhasesController : ControllerBase
{
    private readonly IPhaseService _phaseService;

    public PhasesController(IPhaseService phaseService)
    {
        _phaseService = phaseService;
    }

    /// <summary>List all phases of a session in order.</summary>
    /// <param name="sessionId">Session ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Returns the ordered list of phases.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(Guid sessionId, CancellationToken ct)
        => Ok(await _phaseService.GetSessionPhasesAsync(sessionId, ct));

    /// <summary>Get a single phase by ID.</summary>
    /// <param name="sessionId">Session ID (route context).</param>
    /// <param name="phaseId">Phase ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Returns the phase details.</response>
    [HttpGet("{phaseId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid sessionId, Guid phaseId, CancellationToken ct)
        => Ok(await _phaseService.GetPhaseByIdAsync(phaseId, ct));

    /// <summary>Activate a pending phase. Broadcasts PhaseChanged via SignalR.</summary>
    /// <param name="sessionId">Session ID.</param>
    /// <param name="phaseId">Phase ID to activate.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Phase activated.</response>
    [HttpPost("{phaseId:guid}/activate")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Activate(Guid sessionId, Guid phaseId, CancellationToken ct)
        => Ok(await _phaseService.ActivatePhaseAsync(phaseId, ct));

    /// <summary>Complete an active phase.</summary>
    /// <param name="sessionId">Session ID.</param>
    /// <param name="phaseId">Phase ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Phase completed.</response>
    [HttpPost("{phaseId:guid}/complete")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Complete(Guid sessionId, Guid phaseId, CancellationToken ct)
        => Ok(await _phaseService.CompletePhaseAsync(phaseId, ct));

    /// <summary>Skip a phase entirely.</summary>
    /// <param name="sessionId">Session ID.</param>
    /// <param name="phaseId">Phase ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Phase skipped.</response>
    [HttpPost("{phaseId:guid}/skip")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Skip(Guid sessionId, Guid phaseId, CancellationToken ct)
        => Ok(await _phaseService.SkipPhaseAsync(phaseId, ct));

    /// <summary>Extend the timer of an active phase.</summary>
    /// <param name="sessionId">Session ID.</param>
    /// <param name="phaseId">Phase ID.</param>
    /// <param name="request">Extension duration in minutes.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Phase timer extended.</response>
    [HttpPost("{phaseId:guid}/extend")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Extend(Guid sessionId, Guid phaseId, [FromBody] ExtendPhaseRequest request, CancellationToken ct)
        => Ok(await _phaseService.ExtendPhaseAsync(phaseId, request, ct));

    /// <summary>Advance to the next phase in the session. Completes the current phase and activates the next.</summary>
    /// <param name="sessionId">Session ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Advanced to next phase.</response>
    [HttpPost("advance")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Advance(Guid sessionId, CancellationToken ct)
        => Ok(await _phaseService.AdvanceToNextPhaseAsync(sessionId, ct));

    /// <summary>Reorder phases within a session.</summary>
    /// <param name="sessionId">Session ID.</param>
    /// <param name="request">New phase order.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Phases reordered.</response>
    [HttpPost("reorder")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Reorder(Guid sessionId, [FromBody] ReorderPhasesRequest request, CancellationToken ct)
        => Ok(await _phaseService.ReorderPhasesAsync(sessionId, request, ct));
}
