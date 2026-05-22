using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetroMask.Application.Services.Insights;

namespace RetroMask.API.Controllers.Common;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class InsightsController : ControllerBase
{
    private readonly IInsightService _service;

    public InsightsController(IInsightService service)
    {
        _service = service;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMyInsights(CancellationToken ct)
        => Ok(await _service.GetMyInsightsAsync(ct));

    [HttpGet("me/growth")]
    public async Task<IActionResult> GetGrowthSnapshots([FromQuery] int months = 6, CancellationToken ct = default)
        => Ok(await _service.GetGrowthSnapshotsAsync(months, ct));

    [HttpGet("users/{userId}")]
    public async Task<IActionResult> GetUserInsights(string userId, CancellationToken ct)
        => Ok(await _service.GetUserInsightsAsync(userId, ct));
}
