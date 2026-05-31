using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetroMask.Application.Common;
using RetroMask.Application.Dtos.Reports;
using RetroMask.Application.Services.Reports;

namespace RetroMask.API.Controllers.Common;

/// <summary>
/// Session and team reports: view, export (Markdown/HTML/PDF), and share.
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _service;

    public ReportsController(IReportService service)
    {
        _service = service;
    }

    /// <summary>Get the full report for a completed session.</summary>
    /// <param name="sessionId">Session ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Returns the session report with aggregated data.</response>
    [HttpGet("session/{sessionId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSessionReport(Guid sessionId, CancellationToken ct)
        => Ok(await _service.GetSessionReportAsync(sessionId, ct));

    /// <summary>Get reports across all sessions for a team.</summary>
    /// <param name="teamId">Team ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Returns team-level report data.</response>
    [HttpGet("team/{teamId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTeamReports(Guid teamId, CancellationToken ct)
        => Ok(await _service.GetTeamReportsAsync(teamId, ct));

    /// <summary>Export a session report in the specified format.</summary>
    /// <remarks>Supported formats: markdown (default), html, pdf.</remarks>
    /// <param name="sessionId">Session ID.</param>
    /// <param name="request">Export format and options.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Returns the report file for download.</response>
    /// <response code="400">Export failed or invalid format.</response>
    [HttpPost("session/{sessionId:guid}/export")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Export(Guid sessionId, [FromBody] ExportRequest request, CancellationToken ct)
    {
        var result = await _service.ExportReportAsync(sessionId, request, ct);
        if (!result.Success || result.Data is null || result.Data.Length == 0)
            return BadRequest(result.Success ? ApiResponse.Fail("Export produced no content.") : result);
        var contentType = request.Format switch
        {
            "pdf" => "application/pdf",
            "html" => "text/html",
            _ => "text/markdown"
        };
        return File(result.Data, contentType, $"retro-report.{request.Format}");
    }

    /// <summary>Generate a shareable link for a report.</summary>
    /// <param name="reportId">Report ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Returns the share URL.</response>
    [HttpPost("{reportId:guid}/share")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Share(Guid reportId, CancellationToken ct)
        => Ok(await _service.ShareReportAsync(reportId, ct));
}
