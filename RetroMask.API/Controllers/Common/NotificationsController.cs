using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetroMask.Application.Common;
using RetroMask.Application.Services.Notifications;

namespace RetroMask.API.Controllers.Common;

/// <summary>
/// User notification management: list, unread count, and mark as read.
/// Real-time delivery is handled via SignalR; this controller manages the persisted notification state.
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _service;

    public NotificationsController(INotificationService service)
    {
        _service = service;
    }

    /// <summary>Get the current user's notifications (paginated, newest first).</summary>
    /// <param name="page">Page number (default 1).</param>
    /// <param name="pageSize">Items per page (default 20).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Returns paged notifications.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMine([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await _service.GetMyNotificationsAsync(page, pageSize, ct));

    /// <summary>Get the count of unread notifications.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Returns the unread count as data property.</response>
    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUnreadCount(CancellationToken ct)
        => Ok(await _service.GetUnreadCountAsync(ct));

    /// <summary>Mark a single notification as read.</summary>
    /// <param name="id">Notification ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Notification marked as read.</response>
    [HttpPost("{id:guid}/read")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken ct)
        => Ok(await _service.MarkAsReadAsync(id, ct));

    /// <summary>Mark all notifications as read for the current user.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">All notifications marked as read.</response>
    [HttpPost("read-all")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken ct)
        => Ok(await _service.MarkAllAsReadAsync(ct));
}
