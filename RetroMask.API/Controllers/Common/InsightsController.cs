using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetroMask.Application.Services.Insights;

namespace RetroMask.API.Controllers.Common;

/// <summary>
/// Personal growth insights: engagement scores, participation patterns, and growth snapshots over time.
/// Insights are cached and automatically refreshed when stale (older than 24 hours).
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class InsightsController : ControllerBase
{
    private readonly IInsightService _service;

    public InsightsController(IInsightService service)
    {
        _service = service;
    }

    /// <summary>Get the current user's personal insights (engagement score, participation stats).</summary>
    /// <remarks>Returns cached insights if fresh (under 24h), otherwise generates new ones.</remarks>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Returns the user's insight data.</response>
    [HttpGet("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyInsights(CancellationToken ct)
        => Ok(await _service.GetMyInsightsAsync(ct));

    /// <summary>Get growth snapshots showing engagement trends over time.</summary>
    /// <param name="months">Number of months to look back (default 6).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Returns monthly growth snapshots.</response>
    [HttpGet("me/growth")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGrowthSnapshots([FromQuery] int months = 6, CancellationToken ct = default)
        => Ok(await _service.GetGrowthSnapshotsAsync(months, ct));

    /// <summary>Get insights for a specific user by their ID (admin use).</summary>
    /// <param name="userId">Target user ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Returns the user's insight data.</response>
    [HttpGet("users/{userId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserInsights(string userId, CancellationToken ct)
        => Ok(await _service.GetUserInsightsAsync(userId, ct));

    /// <summary>Force-refresh the current user's insights (bypasses cache).</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Returns freshly computed insights.</response>
    [HttpPost("me/refresh")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RefreshInsights(CancellationToken ct)
        => Ok(await _service.RefreshMyInsightsAsync(ct));
}
