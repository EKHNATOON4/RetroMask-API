using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetroMask.Application.Dtos.Points;
using RetroMask.Application.Services.Points;

namespace RetroMask.API.Controllers.Sessions;

[Authorize]
[ApiController]
[Route("api/phases/{phaseId:guid}/points")]
public class PointsController : ControllerBase
{
    private readonly IPointService _pointService;

    public PointsController(IPointService pointService)
    {
        _pointService = pointService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(Guid phaseId, CancellationToken ct)
        => Ok(await _pointService.GetPhasePointsAsync(phaseId, ct));

    [HttpGet("{pointId:guid}")]
    public async Task<IActionResult> GetById(Guid phaseId, Guid pointId, CancellationToken ct)
        => Ok(await _pointService.GetPointByIdAsync(pointId, ct));

    [HttpPost]
    public async Task<IActionResult> Create(Guid phaseId, [FromBody] CreatePointRequest request, CancellationToken ct)
    {
        var result = await _pointService.CreatePointAsync(phaseId, request, ct);
        return result.Success ? CreatedAtAction(nameof(GetById), new { phaseId, pointId = result.Data!.Id }, result) : BadRequest(result);
    }

    [HttpPut("{pointId:guid}")]
    public async Task<IActionResult> Update(Guid phaseId, Guid pointId, [FromBody] UpdatePointRequest request, CancellationToken ct)
        => Ok(await _pointService.UpdatePointAsync(pointId, request, ct));

    [HttpDelete("{pointId:guid}")]
    public async Task<IActionResult> Delete(Guid phaseId, Guid pointId, CancellationToken ct)
        => Ok(await _pointService.DeletePointAsync(pointId, ct));

    [HttpPost("{pointId:guid}/pin")]
    public async Task<IActionResult> Pin(Guid phaseId, Guid pointId, [FromQuery] bool pin = true, CancellationToken ct = default)
        => Ok(await _pointService.PinPointAsync(pointId, pin, ct));

    [HttpPost("{pointId:guid}/tags")]
    public async Task<IActionResult> AddTag(Guid phaseId, Guid pointId, [FromBody] AddTagRequest request, CancellationToken ct)
        => Ok(await _pointService.AddTagAsync(pointId, request, ct));

    [HttpDelete("{pointId:guid}/tags/{tagId:guid}")]
    public async Task<IActionResult> RemoveTag(Guid phaseId, Guid pointId, Guid tagId, CancellationToken ct)
        => Ok(await _pointService.RemoveTagAsync(pointId, tagId, ct));

    [HttpPost("{pointId:guid}/reactions")]
    public async Task<IActionResult> AddReaction(Guid phaseId, Guid pointId, [FromBody] AddReactionRequest request, CancellationToken ct)
        => Ok(await _pointService.AddReactionAsync(pointId, request, ct));

    [HttpPost("{pointId:guid}/comments")]
    public async Task<IActionResult> AddComment(Guid phaseId, Guid pointId, [FromBody] AddCommentRequest request, CancellationToken ct)
        => Ok(await _pointService.AddCommentAsync(pointId, request, ct));
}
