using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RetroMask.Application.Abstractions;
using RetroMask.Application.Abstractions.Repositories;
using RetroMask.Application.Common;
using RetroMask.Application.Dtos.ActionItems;
using RetroMask.Application.Services.ActionItems;
using RetroMask.Application.Services.Notifications;
using RetroMask.Domain.Entities.ActionItems;
using RetroMask.Domain.Enums;

namespace RetroMask.Infrastructure.Services.ActionItems;

public class ActionItemService : IActionItemService
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    private readonly IMapper _mapper;
    private readonly INotificationService _notifications;

    public ActionItemService(IUnitOfWork uow, ICurrentUser currentUser, IMapper mapper, INotificationService notifications)
    {
        _uow = uow;
        _currentUser = currentUser;
        _mapper = mapper;
        _notifications = notifications;
    }

    public async Task<ApiResponse<ActionItemDto>> CreateAsync(CreateActionItemRequest request, CancellationToken ct = default)
    {
        var item = new ActionItem
        {
            Title = request.Title,
            Description = request.Description,
            SessionId = request.SessionId,
            AssignedToId = request.AssignedToId,
            CreatedById = _currentUser.UserId,
            Priority = request.Priority,
            DueDate = request.DueDate
        };

        await _uow.Repository<ActionItem>().AddAsync(item, ct);
        await _uow.SaveChangesAsync(ct);

        if (request.AssignedToId != _currentUser.UserId)
        {
            await _notifications.SendAsync(request.AssignedToId, new Application.Dtos.Notifications.SendNotificationRequest
            {
                Type = NotificationType.ActionItemAssigned,
                Title = "New Action Item Assigned",
                Message = $"You've been assigned: {request.Title}",
                ActionUrl = $"/action-items/{item.Id}"
            }, ct);
        }

        return await GetByIdAsync(item.Id, ct);
    }

    public async Task<ApiResponse<ActionItemDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _uow.Repository<ActionItem>().Query()
            .Include(i => i.AssignedTo)
            .Include(i => i.Updates).ThenInclude(u => u.Author)
            .FirstOrDefaultAsync(i => i.Id == id, ct);

        if (item is null)
            return ApiResponse<ActionItemDto>.Fail("Action item not found.");

        var dto = _mapper.Map<ActionItemDto>(item);
        dto.ProgressPercent = item.Updates
            .Where(u => u.ProgressPercent.HasValue)
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => u.ProgressPercent!.Value)
            .FirstOrDefault();

        return ApiResponse<ActionItemDto>.Ok(dto);
    }

    public async Task<ApiResponse<PagedResult<ActionItemDto>>> GetSessionActionItemsAsync(Guid sessionId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _uow.Repository<ActionItem>().Query()
            .Where(i => i.SessionId == sessionId)
            .Include(i => i.AssignedTo)
            .Include(i => i.Updates).ThenInclude(u => u.Author)
            .OrderByDescending(i => i.Priority).ThenBy(i => i.DueDate);

        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        var dtos = items.Select(i =>
        {
            var dto = _mapper.Map<ActionItemDto>(i);
            dto.ProgressPercent = i.Updates.Where(u => u.ProgressPercent.HasValue)
                .OrderByDescending(u => u.CreatedAt).Select(u => u.ProgressPercent!.Value).FirstOrDefault();
            return dto;
        });

        return ApiResponse<PagedResult<ActionItemDto>>.Ok(PagedResult<ActionItemDto>.Create(dtos, total, page, pageSize));
    }

    public async Task<ApiResponse<PagedResult<ActionItemDto>>> GetMyActionItemsAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var query = _uow.Repository<ActionItem>().Query()
            .Where(i => i.AssignedToId == _currentUser.UserId)
            .Include(i => i.AssignedTo)
            .Include(i => i.Updates).ThenInclude(u => u.Author)
            .OrderByDescending(i => i.Priority).ThenBy(i => i.DueDate);

        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        var dtos = items.Select(i =>
        {
            var dto = _mapper.Map<ActionItemDto>(i);
            dto.ProgressPercent = i.Updates.Where(u => u.ProgressPercent.HasValue)
                .OrderByDescending(u => u.CreatedAt).Select(u => u.ProgressPercent!.Value).FirstOrDefault();
            return dto;
        });

        return ApiResponse<PagedResult<ActionItemDto>>.Ok(PagedResult<ActionItemDto>.Create(dtos, total, page, pageSize));
    }

    public async Task<ApiResponse<ActionItemDto>> UpdateAsync(Guid id, UpdateActionItemRequest request, CancellationToken ct = default)
    {
        var item = await _uow.Repository<ActionItem>().GetByIdAsync(id, ct);
        if (item is null)
            return ApiResponse<ActionItemDto>.Fail("Action item not found.");

        if (request.Title is not null) item.Title = request.Title;
        if (request.Description is not null) item.Description = request.Description;
        if (request.AssignedToId is not null) item.AssignedToId = request.AssignedToId;
        if (request.Status.HasValue)
        {
            item.Status = request.Status.Value;
            if (request.Status.Value == ActionItemStatus.Done)
                item.CompletedAt = DateTime.UtcNow;
        }
        if (request.Priority.HasValue) item.Priority = request.Priority.Value;
        if (request.DueDate.HasValue) item.DueDate = request.DueDate.Value;

        _uow.Repository<ActionItem>().Update(item);
        await _uow.SaveChangesAsync(ct);

        return await GetByIdAsync(id, ct);
    }

    public async Task<ApiResponse> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _uow.Repository<ActionItem>().GetByIdAsync(id, ct);
        if (item is null)
            return ApiResponse.Fail("Action item not found.");

        item.IsDeleted = true;
        item.DeletedAt = DateTime.UtcNow;
        item.DeletedBy = _currentUser.UserId;
        _uow.Repository<ActionItem>().Update(item);
        await _uow.SaveChangesAsync(ct);

        return ApiResponse.Ok("Action item deleted.");
    }

    public async Task<ApiResponse<ActionItemDto>> AddUpdateAsync(Guid id, AddActionItemUpdateRequest request, CancellationToken ct = default)
    {
        var item = await _uow.Repository<ActionItem>().GetByIdAsync(id, ct);
        if (item is null)
            return ApiResponse<ActionItemDto>.Fail("Action item not found.");

        var update = new ActionItemUpdate
        {
            ActionItemId = id,
            AuthorId = _currentUser.UserId,
            Note = request.Note,
            StatusChange = request.StatusChange,
            ProgressPercent = request.ProgressPercent,
            CreatedBy = _currentUser.UserId
        };

        await _uow.Repository<ActionItemUpdate>().AddAsync(update, ct);

        if (request.StatusChange.HasValue)
        {
            item.Status = request.StatusChange.Value;
            if (request.StatusChange.Value == ActionItemStatus.Done)
                item.CompletedAt = DateTime.UtcNow;
            _uow.Repository<ActionItem>().Update(item);
        }

        await _uow.SaveChangesAsync(ct);

        return await GetByIdAsync(id, ct);
    }
}
