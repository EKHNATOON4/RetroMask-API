using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetroMask.Application.Services.AI;

namespace RetroMask.API.Controllers.Common;

[Authorize]
[ApiController]
[Route("api/sessions/{sessionId:guid}/ai")]
public class AIController : ControllerBase
{
    private readonly IAIService _aiService;

    public AIController(IAIService aiService)
    {
        _aiService = aiService;
    }

    [HttpGet("insights")]
    public async Task<IActionResult> GetInsights(Guid sessionId, CancellationToken ct)
        => Ok(await _aiService.GetSessionInsightsAsync(sessionId, ct));

    [HttpPost("summary")]
    public async Task<IActionResult> GenerateSummary(Guid sessionId, CancellationToken ct)
        => Ok(await _aiService.GenerateSummaryAsync(sessionId, ct));

    [HttpPost("clusters")]
    public async Task<IActionResult> ClusterPoints(Guid sessionId, CancellationToken ct)
        => Ok(await _aiService.ClusterPointsAsync(sessionId, ct));

    [HttpPost("sentiment")]
    public async Task<IActionResult> AnalyzeSentiment(Guid sessionId, CancellationToken ct)
        => Ok(await _aiService.AnalyzeSentimentAsync(sessionId, ct));

    [HttpPost("report")]
    public async Task<IActionResult> GenerateReport(Guid sessionId, CancellationToken ct)
        => Ok(await _aiService.GenerateReportAsync(sessionId, ct));

    [HttpPost("recommendations")]
    public async Task<IActionResult> GenerateRecommendations(Guid sessionId, CancellationToken ct)
        => Ok(await _aiService.GenerateRecommendationsAsync(sessionId, ct));
}
