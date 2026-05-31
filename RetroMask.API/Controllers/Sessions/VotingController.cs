using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetroMask.Application.Common;
using RetroMask.Application.Dtos.Voting;
using RetroMask.Application.Services.Voting;

namespace RetroMask.API.Controllers.Sessions;

/// <summary>
/// Voting on discussion points: cast/remove votes, view summaries, and close voting.
/// Budget enforcement is based on Session.MaxVotesPerUser.
/// </summary>
[Authorize]
[ApiController]
[Route("api/points/{pointId:guid}/votes")]
[Produces("application/json")]
public class VotingController : ControllerBase
{
    private readonly IVotingService _votingService;

    public VotingController(IVotingService votingService)
    {
        _votingService = votingService;
    }

    /// <summary>Get the vote summary for a discussion point (up/down counts, your vote).</summary>
    /// <param name="pointId">Discussion point ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Returns the vote summary.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSummary(Guid pointId, CancellationToken ct)
        => Ok(await _votingService.GetVoteSummaryAsync(pointId, ct));

    /// <summary>Cast an up or down vote on a discussion point.</summary>
    /// <remarks>
    /// Enforces vote budget per session. If the user already voted the same type,
    /// returns 400. If the user changes vote type, the previous vote is replaced.
    /// </remarks>
    /// <param name="pointId">Discussion point ID.</param>
    /// <param name="request">Vote type (Up = 1, Down = -1).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Vote cast successfully.</response>
    /// <response code="400">Budget exhausted, duplicate vote, or voting closed/disabled.</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Cast(Guid pointId, [FromBody] CastVoteRequest request, CancellationToken ct)
    {
        var result = await _votingService.CastVoteAsync(pointId, request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Remove the current user's vote from a discussion point.</summary>
    /// <param name="pointId">Discussion point ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Vote removed.</response>
    [HttpDelete]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Remove(Guid pointId, CancellationToken ct)
        => Ok(await _votingService.RemoveVoteAsync(pointId, ct));

    /// <summary>Close voting on a discussion point (facilitator only).</summary>
    /// <param name="pointId">Discussion point ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Voting closed. No further votes accepted.</response>
    [HttpPost("close")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Close(Guid pointId, CancellationToken ct)
        => Ok(await _votingService.CloseVotingAsync(pointId, ct));
}
