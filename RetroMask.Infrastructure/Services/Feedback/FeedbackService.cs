using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RetroMask.Application.Abstractions;
using RetroMask.Application.Abstractions.Repositories;
using RetroMask.Application.Common;
using RetroMask.Application.Dtos.Feedback;
using RetroMask.Application.Services.Feedback;
using RetroMask.Application.Services.Notifications;
using RetroMask.Domain.Entities.Feedback;
using RetroMask.Domain.Enums;

namespace RetroMask.Infrastructure.Services.Feedback;

public class FeedbackService : IFeedbackService
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    private readonly IMapper _mapper;
    private readonly INotificationService _notifications;

    private static readonly HashSet<string> ToxicPatterns = new(StringComparer.OrdinalIgnoreCase)
    {
        "idiot", "stupid", "hate you", "worthless", "loser", "incompetent", "useless", "terrible person"
    };

    public FeedbackService(IUnitOfWork uow, ICurrentUser currentUser, IMapper mapper, INotificationService notifications)
    {
        _uow = uow;
        _currentUser = currentUser;
        _mapper = mapper;
        _notifications = notifications;
    }

    public async Task<ApiResponse<FeedbackDto>> CreateFeedbackAsync(CreateFeedbackRequest request, CancellationToken ct = default)
    {
        if (request.ReceiverId == _currentUser.UserId)
            return ApiResponse<FeedbackDto>.Fail("You cannot send feedback to yourself.");

        if (ContainsToxicContent(request.Content))
            return ApiResponse<FeedbackDto>.Fail("Your feedback contains inappropriate language. Please revise.");

        var toneType = ClassifyTone(request.Content);

        var feedback = new FriendFeedback
        {
            GiverId = _currentUser.UserId,
            ReceiverId = request.ReceiverId,
            SessionId = request.SessionId,
            Content = request.Content,
            FeedbackType = toneType != FeedbackType.General ? toneType : request.FeedbackType,
            IsAnonymous = request.IsAnonymous,
            Rating = request.Rating,
            CreatedBy = _currentUser.UserId
        };

        await _uow.Repository<FriendFeedback>().AddAsync(feedback, ct);
        await _uow.SaveChangesAsync(ct);

        await _notifications.SendAsync(request.ReceiverId, new Application.Dtos.Notifications.SendNotificationRequest
        {
            Type = NotificationType.FeedbackReceived,
            Title = "New Feedback Received",
            Message = request.IsAnonymous ? "You received anonymous feedback." : $"You received feedback from {_currentUser.DisplayName ?? _currentUser.Email}.",
            ActionUrl = $"/feedback/{feedback.Id}"
        }, ct);

        return await GetFeedbackByIdAsync(feedback.Id, ct);
    }

    public async Task<ApiResponse<FeedbackDto>> GetFeedbackByIdAsync(Guid feedbackId, CancellationToken ct = default)
    {
        var feedback = await _uow.Repository<FriendFeedback>().Query()
            .Include(f => f.Giver)
            .Include(f => f.Receiver)
            .Include(f => f.Reactions)
            .FirstOrDefaultAsync(f => f.Id == feedbackId, ct);

        if (feedback is null)
            return ApiResponse<FeedbackDto>.Fail("Feedback not found.");

        var dto = _mapper.Map<FeedbackDto>(feedback);
        dto.Reactions = feedback.Reactions
            .GroupBy(r => r.ReactionType)
            .Select(g => new FeedbackReactionSummaryDto { ReactionType = g.Key, Count = g.Count() })
            .ToList();

        return ApiResponse<FeedbackDto>.Ok(dto);
    }

    public async Task<ApiResponse<PagedResult<FeedbackDto>>> GetReceivedFeedbackAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var query = _uow.Repository<FriendFeedback>().Query()
            .Where(f => f.ReceiverId == _currentUser.UserId)
            .Include(f => f.Giver)
            .Include(f => f.Receiver)
            .Include(f => f.Reactions)
            .OrderByDescending(f => f.CreatedAt);

        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        var dtos = items.Select(f =>
        {
            var dto = _mapper.Map<FeedbackDto>(f);
            dto.Reactions = f.Reactions.GroupBy(r => r.ReactionType)
                .Select(g => new FeedbackReactionSummaryDto { ReactionType = g.Key, Count = g.Count() }).ToList();
            return dto;
        });

        return ApiResponse<PagedResult<FeedbackDto>>.Ok(PagedResult<FeedbackDto>.Create(dtos, total, page, pageSize));
    }

    public async Task<ApiResponse<PagedResult<FeedbackDto>>> GetGivenFeedbackAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var query = _uow.Repository<FriendFeedback>().Query()
            .Where(f => f.GiverId == _currentUser.UserId)
            .Include(f => f.Giver)
            .Include(f => f.Receiver)
            .Include(f => f.Reactions)
            .OrderByDescending(f => f.CreatedAt);

        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        var dtos = items.Select(f =>
        {
            var dto = _mapper.Map<FeedbackDto>(f);
            dto.Reactions = f.Reactions.GroupBy(r => r.ReactionType)
                .Select(g => new FeedbackReactionSummaryDto { ReactionType = g.Key, Count = g.Count() }).ToList();
            return dto;
        });

        return ApiResponse<PagedResult<FeedbackDto>>.Ok(PagedResult<FeedbackDto>.Create(dtos, total, page, pageSize));
    }

    public async Task<ApiResponse> DeleteFeedbackAsync(Guid feedbackId, CancellationToken ct = default)
    {
        var feedback = await _uow.Repository<FriendFeedback>().GetByIdAsync(feedbackId, ct);
        if (feedback is null)
            return ApiResponse.Fail("Feedback not found.");

        if (feedback.GiverId != _currentUser.UserId)
            return ApiResponse.Fail("Only the giver can delete feedback.");

        feedback.IsDeleted = true;
        feedback.DeletedAt = DateTime.UtcNow;
        feedback.DeletedBy = _currentUser.UserId;
        _uow.Repository<FriendFeedback>().Update(feedback);
        await _uow.SaveChangesAsync(ct);

        return ApiResponse.Ok("Feedback deleted.");
    }

    public async Task<ApiResponse> AddReactionAsync(Guid feedbackId, AddFeedbackReactionRequest request, CancellationToken ct = default)
    {
        var exists = await _uow.Repository<FriendFeedback>().AnyAsync(f => f.Id == feedbackId, ct);
        if (!exists)
            return ApiResponse.Fail("Feedback not found.");

        var existing = await _uow.Repository<FeedbackReaction>().FirstOrDefaultAsync(
            r => r.FriendFeedbackId == feedbackId && r.UserId == _currentUser.UserId && r.ReactionType == request.ReactionType, ct);

        if (existing is not null)
        {
            _uow.Repository<FeedbackReaction>().Remove(existing);
            await _uow.SaveChangesAsync(ct);
            return ApiResponse.Ok("Reaction removed.");
        }

        var reaction = new FeedbackReaction
        {
            FriendFeedbackId = feedbackId,
            UserId = _currentUser.UserId,
            ReactionType = request.ReactionType
        };

        await _uow.Repository<FeedbackReaction>().AddAsync(reaction, ct);
        await _uow.SaveChangesAsync(ct);

        return ApiResponse.Ok("Reaction added.");
    }

    private static bool ContainsToxicContent(string content)
    {
        var lower = content.ToLowerInvariant();
        return ToxicPatterns.Any(p => lower.Contains(p));
    }

    private static FeedbackType ClassifyTone(string content)
    {
        var lower = content.ToLowerInvariant();
        var praiseWords = new[] { "great", "awesome", "excellent", "amazing", "fantastic", "wonderful", "kudos", "thank", "appreciate", "outstanding", "brilliant" };
        var constructiveWords = new[] { "improve", "better", "consider", "suggest", "could", "should", "try", "next time", "opportunity", "challenge" };

        var praiseScore = praiseWords.Count(w => lower.Contains(w));
        var constructiveScore = constructiveWords.Count(w => lower.Contains(w));

        if (praiseScore > constructiveScore && praiseScore >= 2) return FeedbackType.Praise;
        if (constructiveScore > praiseScore && constructiveScore >= 2) return FeedbackType.Constructive;
        return FeedbackType.General;
    }
}
