using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RetroMask.Application.Abstractions;
using RetroMask.Application.Abstractions.Repositories;
using RetroMask.Application.Common;
using RetroMask.Application.Dtos.Notifications;
using RetroMask.Application.Services.Notifications;
using RetroMask.Domain.Entities.Notifications;

namespace RetroMask.Infrastructure.Services.Notifications;

public class NotificationService : INotificationService
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    private readonly IMapper _mapper;
    private readonly ISessionBroadcaster _broadcaster;

    public NotificationService(IUnitOfWork uow, ICurrentUser currentUser, IMapper mapper, ISessionBroadcaster broadcaster)
    {
        _uow = uow;
        _currentUser = currentUser;
        _mapper = mapper;
        _broadcaster = broadcaster;
    }

    public async Task<ApiResponse<PagedResult<NotificationDto>>> GetMyNotificationsAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var query = _uow.Repository<Notification>().Query()
            .Where(n => n.UserId == _currentUser.UserId)
            .OrderByDescending(n => n.CreatedAt);

        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        var dtos = _mapper.Map<IEnumerable<NotificationDto>>(items);
        return ApiResponse<PagedResult<NotificationDto>>.Ok(PagedResult<NotificationDto>.Create(dtos, total, page, pageSize));
    }

    public async Task<ApiResponse<int>> GetUnreadCountAsync(CancellationToken ct = default)
    {
        var count = await _uow.Repository<Notification>().CountAsync(
            n => n.UserId == _currentUser.UserId && !n.IsRead, ct);

        return ApiResponse<int>.Ok(count);
    }

    public async Task<ApiResponse> MarkAsReadAsync(Guid notificationId, CancellationToken ct = default)
    {
        var notification = await _uow.Repository<Notification>().FirstOrDefaultAsync(
            n => n.Id == notificationId && n.UserId == _currentUser.UserId, ct);

        if (notification is null)
            return ApiResponse.Fail("Notification not found.");

        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;
        _uow.Repository<Notification>().Update(notification);
        await _uow.SaveChangesAsync(ct);

        return ApiResponse.Ok("Marked as read.");
    }

    public async Task<ApiResponse> MarkAllAsReadAsync(CancellationToken ct = default)
    {
        var unread = await _uow.Repository<Notification>().FindAsync(
            n => n.UserId == _currentUser.UserId && !n.IsRead, ct);

        foreach (var n in unread)
        {
            n.IsRead = true;
            n.ReadAt = DateTime.UtcNow;
            _uow.Repository<Notification>().Update(n);
        }

        await _uow.SaveChangesAsync(ct);

        return ApiResponse.Ok("All notifications marked as read.");
    }

    public async Task SendAsync(string userId, SendNotificationRequest request, CancellationToken ct = default)
    {
        var notification = new Notification
        {
            UserId = userId,
            Type = request.Type,
            Title = request.Title,
            Message = request.Message,
            ActionUrl = request.ActionUrl,
            MetadataJson = request.MetadataJson
        };

        await _uow.Repository<Notification>().AddAsync(notification, ct);
        await _uow.SaveChangesAsync(ct);

        await _broadcaster.BroadcastToUserAsync(userId, "NotificationReceived", new
        {
            notification.Id,
            notification.Type,
            notification.Title,
            notification.Message,
            notification.ActionUrl,
            notification.CreatedAt
        }, ct);
    }
}
