using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetroMask.Application.Common;
using RetroMask.Application.Dtos.Points;
using RetroMask.Application.Services.Points;

namespace RetroMask.API.Controllers.Sessions;

/// <summary>
/// Discussion points within a session phase: CRUD, tags, reactions, and comments.
/// </summary>
[Authorize]
[ApiController]
[Route("api/phases/{phaseId:guid}/points")]
[Produces("application/json")]
public class PointsController : ControllerBase
{
    private readonly IPointService _pointService;

    public PointsController(IPointService pointService)
    {
        _pointService = pointService;
    }

    /// <summary>List all discussion points in a phase.</summary>
    /// <param name="phaseId">Phase ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Returns the points.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(Guid phaseId, CancellationToken ct)
        => Ok(await _pointService.GetPhasePointsAsync(phaseId, ct));

    /// <summary>Get a single discussion point by ID.</summary>
    /// <param name="phaseId">Phase ID (route context).</param>
    /// <param name="pointId">Discussion point ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Returns the point.</response>
    [HttpGet("{pointId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid phaseId, Guid pointId, CancellationToken ct)
        => Ok(await _pointService.GetPointByIdAsync(pointId, ct));

    /// <summary>Create a new discussion point in a phase.</summary>
    /// <param name="phaseId">Phase ID.</param>
    /// <param name="request">Point content.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="201">Point created.</response>
    /// <response code="400">Validation failed or toxic content detected.</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(Guid phaseId, [FromBody] CreatePointRequest request, CancellationToken ct)
    {
        var result = await _pointService.CreatePointAsync(phaseId, request, ct);
        if (!result.Success || result.Data is null) return BadRequest(result);
        return CreatedAtAction(nameof(GetById), new { phaseId, pointId = result.Data.Id }, result);
    }

    /// <summary>Update a discussion point's content.</summary>
    /// <param name="phaseId">Phase ID.</param>
    /// <param name="pointId">Point ID.</param>
    /// <param name="request">Updated content.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Point updated.</response>
    [HttpPut("{pointId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid phaseId, Guid pointId, [FromBody] UpdatePointRequest request, CancellationToken ct)
        => Ok(await _pointService.UpdatePointAsync(pointId, request, ct));

    /// <summary>Soft-delete a discussion point.</summary>
    /// <param name="phaseId">Phase ID.</param>
    /// <param name="pointId">Point ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Point deleted.</response>
    [HttpDelete("{pointId:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(Guid phaseId, Guid pointId, CancellationToken ct)
        => Ok(await _pointService.DeletePointAsync(pointId, ct));

    /// <summary>Pin or unpin a discussion point for emphasis.</summary>
    /// <param name="phaseId">Phase ID.</param>
    /// <param name="pointId">Point ID.</param>
    /// <param name="pin">True to pin, false to unpin (default true).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Pin status updated.</response>
    [HttpPost("{pointId:guid}/pin")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Pin(Guid phaseId, Guid pointId, [FromQuery] bool pin = true, CancellationToken ct = default)
        => Ok(await _pointService.PinPointAsync(pointId, pin, ct));

    /// <summary>Add a tag to a discussion point.</summary>
    /// <param name="phaseId">Phase ID.</param>
    /// <param name="pointId">Point ID.</param>
    /// <param name="request">Tag text.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Tag added.</response>
    [HttpPost("{pointId:guid}/tags")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> AddTag(Guid phaseId, Guid pointId, [FromBody] AddTagRequest request, CancellationToken ct)
        => Ok(await _pointService.AddTagAsync(pointId, request, ct));

    /// <summary>Remove a tag from a discussion point.</summary>
    /// <param name="phaseId">Phase ID.</param>
    /// <param name="pointId">Point ID.</param>
    /// <param name="tagId">Tag ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Tag removed.</response>
    [HttpDelete("{pointId:guid}/tags/{tagId:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> RemoveTag(Guid phaseId, Guid pointId, Guid tagId, CancellationToken ct)
        => Ok(await _pointService.RemoveTagAsync(pointId, tagId, ct));

    /// <summary>Add an emoji reaction to a discussion point.</summary>
    /// <param name="phaseId">Phase ID.</param>
    /// <param name="pointId">Point ID.</param>
    /// <param name="request">Reaction emoji.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Reaction added.</response>
    [HttpPost("{pointId:guid}/reactions")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> AddReaction(Guid phaseId, Guid pointId, [FromBody] AddReactionRequest request, CancellationToken ct)
        => Ok(await _pointService.AddReactionAsync(pointId, request, ct));

    /// <summary>Add a threaded comment to a discussion point.</summary>
    /// <param name="phaseId">Phase ID.</param>
    /// <param name="pointId">Point ID.</param>
    /// <param name="request">Comment text.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Comment added.</response>
    [HttpPost("{pointId:guid}/comments")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> AddComment(Guid phaseId, Guid pointId, [FromBody] AddCommentRequest request, CancellationToken ct)
        => Ok(await _pointService.AddCommentAsync(pointId, request, ct));
}
