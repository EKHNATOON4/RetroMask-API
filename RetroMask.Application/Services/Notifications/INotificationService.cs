using RetroMask.Application.Common;
using RetroMask.Application.Dtos.Notifications;

namespace RetroMask.Application.Services.Notifications;

public interface INotificationService
{
    Task<ApiResponse<PagedResult<NotificationDto>>> GetMyNotificationsAsync(int page, int pageSize, CancellationToken ct = default);
    Task<ApiResponse<int>> GetUnreadCountAsync(CancellationToken ct = default);
    Task<ApiResponse> MarkAsReadAsync(Guid notificationId, CancellationToken ct = default);
    Task<ApiResponse> MarkAllAsReadAsync(CancellationToken ct = default);
    Task SendAsync(string userId, Dtos.Notifications.SendNotificationRequest request, CancellationToken ct = default);
}
