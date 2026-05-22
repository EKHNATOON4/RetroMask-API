using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetroMask.Application.Dtos.Sessions;
using RetroMask.Application.Services.Phases;

namespace RetroMask.API.Controllers.Sessions;

[Authorize]
[ApiController]
[Route("api/sessions/{sessionId:guid}/phases")]
public class PhasesController : ControllerBase
{
    private readonly IPhaseService _phaseService;

    public PhasesController(IPhaseService phaseService)
    {
        _phaseService = phaseService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(Guid sessionId, CancellationToken ct)
        => Ok(await _phaseService.GetSessionPhasesAsync(sessionId, ct));

    [HttpGet("{phaseId:guid}")]
    public async Task<IActionResult> GetById(Guid sessionId, Guid phaseId, CancellationToken ct)
        => Ok(await _phaseService.GetPhaseByIdAsync(phaseId, ct));

    [HttpPost("{phaseId:guid}/activate")]
    public async Task<IActionResult> Activate(Guid sessionId, Guid phaseId, CancellationToken ct)
        => Ok(await _phaseService.ActivatePhaseAsync(phaseId, ct));

    [HttpPost("{phaseId:guid}/complete")]
    public async Task<IActionResult> Complete(Guid sessionId, Guid phaseId, CancellationToken ct)
        => Ok(await _phaseService.CompletePhaseAsync(phaseId, ct));

    [HttpPost("{phaseId:guid}/skip")]
    public async Task<IActionResult> Skip(Guid sessionId, Guid phaseId, CancellationToken ct)
        => Ok(await _phaseService.SkipPhaseAsync(phaseId, ct));

    [HttpPost("reorder")]
    public async Task<IActionResult> Reorder(Guid sessionId, [FromBody] ReorderPhasesRequest request, CancellationToken ct)
        => Ok(await _phaseService.ReorderPhasesAsync(sessionId, request, ct));
}
