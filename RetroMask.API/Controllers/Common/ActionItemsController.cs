using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetroMask.Application.Common;
using RetroMask.Application.Dtos.ActionItems;
using RetroMask.Application.Services.ActionItems;

namespace RetroMask.API.Controllers.Common;

/// <summary>
/// Action items created from retrospective sessions: CRUD and progress updates.
/// Notifies the assigned user when a new action item is created.
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ActionItemsController : ControllerBase
{
    private readonly IActionItemService _service;

    public ActionItemsController(IActionItemService service)
    {
        _service = service;
    }

    /// <summary>Get action items assigned to the current user (paginated).</summary>
    /// <param name="page">Page number (default 1).</param>
    /// <param name="pageSize">Items per page (default 20).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Returns paged list of action items.</response>
    [HttpGet("mine")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMine([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await _service.GetMyActionItemsAsync(page, pageSize, ct));

    /// <summary>Get all action items for a specific session (paginated).</summary>
    /// <param name="sessionId">Session ID.</param>
    /// <param name="page">Page number.</param>
    /// <param name="pageSize">Items per page.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Returns paged list of session action items.</response>
    [HttpGet("session/{sessionId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBySession(Guid sessionId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await _service.GetSessionActionItemsAsync(sessionId, page, pageSize, ct));

    /// <summary>Get a single action item by ID.</summary>
    /// <param name="id">Action item ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Returns the action item.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => Ok(await _service.GetByIdAsync(id, ct));

    /// <summary>Create a new action item. Notifies the assignee if different from creator.</summary>
    /// <param name="request">Action item details (title, assignee, priority, due date).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="201">Action item created.</response>
    /// <response code="400">Validation failed.</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateActionItemRequest request, CancellationToken ct)
    {
        var result = await _service.CreateAsync(request, ct);
        if (!result.Success || result.Data is null) return BadRequest(result);
        return CreatedAtAction(nameof(GetById), new { id = result.Data.Id }, result);
    }

    /// <summary>Update an action item (title, status, priority, due date).</summary>
    /// <remarks>Setting status to Done automatically records CompletedAt.</remarks>
    /// <param name="id">Action item ID.</param>
    /// <param name="request">Fields to update.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Action item updated.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateActionItemRequest request, CancellationToken ct)
        => Ok(await _service.UpdateAsync(id, request, ct));

    /// <summary>Soft-delete an action item.</summary>
    /// <param name="id">Action item ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Action item deleted.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        => Ok(await _service.DeleteAsync(id, ct));

    /// <summary>Add a progress update to an action item with optional status change.</summary>
    /// <param name="id">Action item ID.</param>
    /// <param name="request">Update note, optional status change, and progress percent.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Update recorded.</response>
    [HttpPost("{id:guid}/updates")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> AddUpdate(Guid id, [FromBody] AddActionItemUpdateRequest request, CancellationToken ct)
        => Ok(await _service.AddUpdateAsync(id, request, ct));
}
