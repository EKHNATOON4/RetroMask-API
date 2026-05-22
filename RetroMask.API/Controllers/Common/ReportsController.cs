using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetroMask.Application.Dtos.Reports;
using RetroMask.Application.Services.Reports;

namespace RetroMask.API.Controllers.Common;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _service;

    public ReportsController(IReportService service)
    {
        _service = service;
    }

    [HttpGet("session/{sessionId:guid}")]
    public async Task<IActionResult> GetSessionReport(Guid sessionId, CancellationToken ct)
        => Ok(await _service.GetSessionReportAsync(sessionId, ct));

    [HttpGet("team/{teamId:guid}")]
    public async Task<IActionResult> GetTeamReports(Guid teamId, CancellationToken ct)
        => Ok(await _service.GetTeamReportsAsync(teamId, ct));

    [HttpPost("session/{sessionId:guid}/export")]
    public async Task<IActionResult> Export(Guid sessionId, [FromBody] ExportRequest request, CancellationToken ct)
    {
        var result = await _service.ExportReportAsync(sessionId, request, ct);
        if (!result.Success) return BadRequest(result);
        var contentType = request.Format switch
        {
            "pdf" => "application/pdf",
            "html" => "text/html",
            _ => "text/markdown"
        };
        return File(result.Data!, contentType, $"retro-report.{request.Format}");
    }

    [HttpPost("{reportId:guid}/share")]
    public async Task<IActionResult> Share(Guid reportId, CancellationToken ct)
        => Ok(await _service.ShareReportAsync(reportId, ct));
}
