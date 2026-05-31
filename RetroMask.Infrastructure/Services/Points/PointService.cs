using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RetroMask.Application.Abstractions;
using RetroMask.Application.Abstractions.Repositories;
using RetroMask.Application.Common;
using RetroMask.Application.Dtos.Points;
using RetroMask.Application.Services.Points;
using RetroMask.Domain.Entities.Discussion;
using RetroMask.Domain.Entities.Sessions;
using RetroMask.Domain.Enums;

namespace RetroMask.Infrastructure.Services.Points;

public class PointService : IPointService
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    private readonly IMapper _mapper;
    private readonly ISessionBroadcaster _broadcaster;

    public PointService(IUnitOfWork uow, ICurrentUser currentUser, IMapper mapper, ISessionBroadcaster broadcaster)
    {
        _uow = uow;
        _currentUser = currentUser;
        _mapper = mapper;
        _broadcaster = broadcaster;
    }

    public async Task<ApiResponse<PointDto>> CreatePointAsync(Guid phaseId, CreatePointRequest request, CancellationToken ct = default)
    {
        var phase = await _uow.Repository<SessionPhase>().Query()
            .Include(p => p.Session)
            .FirstOrDefaultAsync(p => p.Id == phaseId, ct);

        if (phase is null)
            return ApiResponse<PointDto>.Fail("Phase not found.");

        if (phase.Session is null)
            return ApiResponse<PointDto>.Fail("Session context not found for this phase.");

        if (phase.Status != PhaseStatus.Active)
            return ApiResponse<PointDto>.Fail("Points can only be added to active phases.");

        var isAnonymous = request.IsAnonymous || phase.Session.IsAnonymous;

        var point = new DiscussionPoint
        {
            PhaseId = phaseId,
            AuthorId = _currentUser.UserId,
            Content = request.Content,
            PointType = request.PointType,
            IsAnonymous = isAnonymous,
            CreatedBy = _currentUser.UserId
        };

        await _uow.Repository<DiscussionPoint>().AddAsync(point, ct);
        await _uow.SaveChangesAsync(ct);

        var dto = await GetPointDtoAsync(point.Id, ct);
        if (dto is null)
            return ApiResponse<PointDto>.Fail("Failed to retrieve created point.");

        await _broadcaster.NotifyPointAddedAsync(phase.SessionId, dto, ct);

        return ApiResponse<PointDto>.Ok(dto);
    }

    public async Task<ApiResponse<PointDto>> GetPointByIdAsync(Guid pointId, CancellationToken ct = default)
    {
        var dto = await GetPointDtoAsync(pointId, ct);
        return dto is null
            ? ApiResponse<PointDto>.Fail("Point not found.")
            : ApiResponse<PointDto>.Ok(dto);
    }

    public async Task<ApiResponse<IEnumerable<PointDto>>> GetPhasePointsAsync(Guid phaseId, CancellationToken ct = default)
    {
        var points = await _uow.Repository<DiscussionPoint>().Query()
            .Where(p => p.PhaseId == phaseId)
            .Include(p => p.Author)
            .Include(p => p.Tags)
            .Include(p => p.Reactions)
            .Include(p => p.Votes)
            .Include(p => p.Comments)
            .OrderBy(p => p.Order).ThenBy(p => p.CreatedAt)
            .ToListAsync(ct);

        var dtos = points.Select(p => MapPointDto(p)).ToList();
        return ApiResponse<IEnumerable<PointDto>>.Ok(dtos);
    }

    public async Task<ApiResponse<PointDto>> UpdatePointAsync(Guid pointId, UpdatePointRequest request, CancellationToken ct = default)
    {
        var point = await _uow.Repository<DiscussionPoint>().GetByIdAsync(pointId, ct);
        if (point is null)
            return ApiResponse<PointDto>.Fail("Point not found.");

        if (point.AuthorId != _currentUser.UserId)
            return ApiResponse<PointDto>.Fail("Only the author can update this point.");

        if (request.Content is not null) point.Content = request.Content;
        if (request.PointType.HasValue) point.PointType = request.PointType.Value;

        _uow.Repository<DiscussionPoint>().Update(point);
        await _uow.SaveChangesAsync(ct);

        var dto = await GetPointDtoAsync(pointId, ct);
        if (dto is null)
            return ApiResponse<PointDto>.Fail("Failed to retrieve updated point.");

        return ApiResponse<PointDto>.Ok(dto);
    }

    public async Task<ApiResponse> DeletePointAsync(Guid pointId, CancellationToken ct = default)
    {
        var point = await _uow.Repository<DiscussionPoint>().GetByIdAsync(pointId, ct);
        if (point is null)
            return ApiResponse.Fail("Point not found.");

        if (point.AuthorId != _currentUser.UserId)
            return ApiResponse.Fail("Only the author can delete this point.");

        point.IsDeleted = true;
        point.DeletedAt = DateTime.UtcNow;
        point.DeletedBy = _currentUser.UserId;
        _uow.Repository<DiscussionPoint>().Update(point);
        await _uow.SaveChangesAsync(ct);

        return ApiResponse.Ok("Point deleted.");
    }

    public async Task<ApiResponse<PointDto>> PinPointAsync(Guid pointId, bool pin, CancellationToken ct = default)
    {
        var point = await _uow.Repository<DiscussionPoint>().GetByIdAsync(pointId, ct);
        if (point is null)
            return ApiResponse<PointDto>.Fail("Point not found.");

        point.IsPinned = pin;
        _uow.Repository<DiscussionPoint>().Update(point);
        await _uow.SaveChangesAsync(ct);

        var dto = await GetPointDtoAsync(pointId, ct);
        if (dto is null)
            return ApiResponse<PointDto>.Fail("Failed to retrieve point.");

        return ApiResponse<PointDto>.Ok(dto);
    }

    public async Task<ApiResponse> AddTagAsync(Guid pointId, AddTagRequest request, CancellationToken ct = default)
    {
        var exists = await _uow.Repository<DiscussionPoint>().AnyAsync(p => p.Id == pointId, ct);
        if (!exists)
            return ApiResponse.Fail("Point not found.");

        var tag = new PointTag
        {
            DiscussionPointId = pointId,
            Label = request.Label,
            ColorHex = request.ColorHex
        };

        await _uow.Repository<PointTag>().AddAsync(tag, ct);
        await _uow.SaveChangesAsync(ct);

        return ApiResponse.Ok("Tag added.");
    }

    public async Task<ApiResponse> RemoveTagAsync(Guid pointId, Guid tagId, CancellationToken ct = default)
    {
        var tag = await _uow.Repository<PointTag>().FirstOrDefaultAsync(
            t => t.Id == tagId && t.DiscussionPointId == pointId, ct);

        if (tag is null)
            return ApiResponse.Fail("Tag not found.");

        _uow.Repository<PointTag>().Remove(tag);
        await _uow.SaveChangesAsync(ct);

        return ApiResponse.Ok("Tag removed.");
    }

    public async Task<ApiResponse> AddReactionAsync(Guid pointId, AddReactionRequest request, CancellationToken ct = default)
    {
        var exists = await _uow.Repository<DiscussionPoint>().AnyAsync(p => p.Id == pointId, ct);
        if (!exists)
            return ApiResponse.Fail("Point not found.");

        var existing = await _uow.Repository<PointReaction>().FirstOrDefaultAsync(
            r => r.DiscussionPointId == pointId && r.UserId == _currentUser.UserId && r.ReactionType == request.ReactionType, ct);

        if (existing is not null)
        {
            _uow.Repository<PointReaction>().Remove(existing);
            await _uow.SaveChangesAsync(ct);
            return ApiResponse.Ok("Reaction removed.");
        }

        var reaction = new PointReaction
        {
            DiscussionPointId = pointId,
            UserId = _currentUser.UserId,
            ReactionType = request.ReactionType
        };

        await _uow.Repository<PointReaction>().AddAsync(reaction, ct);
        await _uow.SaveChangesAsync(ct);

        return ApiResponse.Ok("Reaction added.");
    }

    public async Task<ApiResponse<CommentDto>> AddCommentAsync(Guid pointId, AddCommentRequest request, CancellationToken ct = default)
    {
        var point = await _uow.Repository<DiscussionPoint>().Query()
            .Include(p => p.Phase).ThenInclude(ph => ph.Session)
            .FirstOrDefaultAsync(p => p.Id == pointId, ct);

        if (point is null)
            return ApiResponse<CommentDto>.Fail("Point not found.");

        if (point.Phase?.Session is null)
            return ApiResponse<CommentDto>.Fail("Session context not found for this point.");

        var isAnonymous = request.IsAnonymous || point.Phase.Session.IsAnonymous;

        var comment = new DiscussionComment
        {
            DiscussionPointId = pointId,
            AuthorId = _currentUser.UserId,
            Content = request.Content,
            IsAnonymous = isAnonymous,
            ParentCommentId = request.ParentCommentId,
            CreatedBy = _currentUser.UserId
        };

        await _uow.Repository<DiscussionComment>().AddAsync(comment, ct);
        await _uow.SaveChangesAsync(ct);

        var loaded = await _uow.Repository<DiscussionComment>().Query()
            .Include(c => c.Author)
            .FirstOrDefaultAsync(c => c.Id == comment.Id, ct);

        var dto = _mapper.Map<CommentDto>(loaded);
        return ApiResponse<CommentDto>.Ok(dto);
    }

    private async Task<PointDto?> GetPointDtoAsync(Guid pointId, CancellationToken ct)
    {
        var point = await _uow.Repository<DiscussionPoint>().Query()
            .Include(p => p.Author)
            .Include(p => p.Tags)
            .Include(p => p.Reactions)
            .Include(p => p.Votes)
            .Include(p => p.Comments)
            .FirstOrDefaultAsync(p => p.Id == pointId, ct);

        return point is null ? null : MapPointDto(point);
    }

    private PointDto MapPointDto(DiscussionPoint point)
    {
        var dto = _mapper.Map<PointDto>(point);
        dto.Reactions = point.Reactions
            .GroupBy(r => r.ReactionType)
            .Select(g => new ReactionSummaryDto
            {
                ReactionType = g.Key,
                Count = g.Count(),
                ReactedByMe = g.Any(r => r.UserId == _currentUser.UserId)
            }).ToList();
        return dto;
    }
}
