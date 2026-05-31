using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetroMask.Application.Services.AI;

namespace RetroMask.API.Controllers.Common;

/// <summary>
/// AI-powered session analysis: insights, summaries, clustering, sentiment, and recommendations.
/// Uses OpenAI GPT-4o-mini with graceful fallback when the API key is unavailable.
/// </summary>
[Authorize]
[ApiController]
[Route("api/sessions/{sessionId:guid}/ai")]
[Produces("application/json")]
public class AIController : ControllerBase
{
    private readonly IAIService _aiService;

    public AIController(IAIService aiService)
    {
        _aiService = aiService;
    }

    /// <summary>Get AI-generated insights for a session's discussion points.</summary>
    /// <param name="sessionId">Session ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Returns AI insights (themes, patterns, sentiment).</response>
    [HttpGet("insights")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInsights(Guid sessionId, CancellationToken ct)
        => Ok(await _aiService.GetSessionInsightsAsync(sessionId, ct));

    /// <summary>Generate an AI summary of the entire session.</summary>
    /// <param name="sessionId">Session ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Returns a concise session summary.</response>
    [HttpPost("summary")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GenerateSummary(Guid sessionId, CancellationToken ct)
        => Ok(await _aiService.GenerateSummaryAsync(sessionId, ct));

    /// <summary>Cluster discussion points into thematic groups using AI.</summary>
    /// <param name="sessionId">Session ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Returns clustered groups of related points.</response>
    [HttpPost("clusters")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ClusterPoints(Guid sessionId, CancellationToken ct)
        => Ok(await _aiService.ClusterPointsAsync(sessionId, ct));

    /// <summary>Analyze overall sentiment of session discussion points.</summary>
    /// <param name="sessionId">Session ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Returns sentiment breakdown (positive/negative/neutral).</response>
    [HttpPost("sentiment")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> AnalyzeSentiment(Guid sessionId, CancellationToken ct)
        => Ok(await _aiService.AnalyzeSentimentAsync(sessionId, ct));

    /// <summary>Generate a comprehensive AI report for the session.</summary>
    /// <param name="sessionId">Session ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Returns a detailed analysis report.</response>
    [HttpPost("report")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GenerateReport(Guid sessionId, CancellationToken ct)
        => Ok(await _aiService.GenerateReportAsync(sessionId, ct));

    /// <summary>Get AI-generated action recommendations based on session content.</summary>
    /// <param name="sessionId">Session ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Returns prioritized recommendations.</response>
    [HttpPost("recommendations")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GenerateRecommendations(Guid sessionId, CancellationToken ct)
        => Ok(await _aiService.GenerateRecommendationsAsync(sessionId, ct));
}
