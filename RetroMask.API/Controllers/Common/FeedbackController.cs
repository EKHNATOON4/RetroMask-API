using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetroMask.Application.Common;
using RetroMask.Application.Dtos.Feedback;
using RetroMask.Application.Services.Feedback;

namespace RetroMask.API.Controllers.Common;

/// <summary>
/// Anonymous peer feedback: give/receive feedback with tone classification and toxic content filtering.
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class FeedbackController : ControllerBase
{
    private readonly IFeedbackService _service;

    public FeedbackController(IFeedbackService service)
    {
        _service = service;
    }

    /// <summary>Get feedback received by the current user (paginated).</summary>
    /// <param name="page">Page number.</param>
    /// <param name="pageSize">Items per page.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Returns paged list of received feedback.</response>
    [HttpGet("received")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReceived([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await _service.GetReceivedFeedbackAsync(page, pageSize, ct));

    /// <summary>Get feedback given by the current user (paginated).</summary>
    /// <param name="page">Page number.</param>
    /// <param name="pageSize">Items per page.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Returns paged list of given feedback.</response>
    [HttpGet("given")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGiven([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await _service.GetGivenFeedbackAsync(page, pageSize, ct));

    /// <summary>Get a single feedback entry by ID.</summary>
    /// <param name="id">Feedback ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Returns the feedback.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => Ok(await _service.GetFeedbackByIdAsync(id, ct));

    /// <summary>Send anonymous feedback to a peer. Auto-classifies tone (Praise/Constructive/General).</summary>
    /// <remarks>
    /// Content is checked for toxic language and rejected if detected.
    /// Users cannot send feedback to themselves.
    /// The receiver is notified via the notification system.
    /// </remarks>
    /// <param name="request">Feedback content, receiver ID, and session ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="201">Feedback sent and receiver notified.</response>
    /// <response code="400">Toxic content detected, self-feedback, or validation failed.</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateFeedbackRequest request, CancellationToken ct)
    {
        var result = await _service.CreateFeedbackAsync(request, ct);
        if (!result.Success || result.Data is null) return BadRequest(result);
        return CreatedAtAction(nameof(GetById), new { id = result.Data.Id }, result);
    }

    /// <summary>Delete feedback you gave (giver only).</summary>
    /// <param name="id">Feedback ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Feedback deleted.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        => Ok(await _service.DeleteFeedbackAsync(id, ct));

    /// <summary>Add an emoji reaction to a feedback entry.</summary>
    /// <param name="id">Feedback ID.</param>
    /// <param name="request">Reaction emoji.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Reaction added.</response>
    [HttpPost("{id:guid}/reactions")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> AddReaction(Guid id, [FromBody] AddFeedbackReactionRequest request, CancellationToken ct)
        => Ok(await _service.AddReactionAsync(id, request, ct));
}
