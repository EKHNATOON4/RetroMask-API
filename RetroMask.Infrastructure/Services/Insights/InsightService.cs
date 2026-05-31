using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RetroMask.Application.Abstractions;
using RetroMask.Application.Abstractions.Repositories;
using RetroMask.Application.Common;
using RetroMask.Application.Dtos.Insights;
using RetroMask.Application.Services.Insights;
using RetroMask.Domain.Entities.ActionItems;
using RetroMask.Domain.Entities.Discussion;
using RetroMask.Domain.Entities.Insights;
using RetroMask.Domain.Entities.Sessions;
using RetroMask.Domain.Entities.Voting;
using RetroMask.Domain.Enums;
using System.Globalization;

namespace RetroMask.Infrastructure.Services.Insights;

public class InsightService : IInsightService
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    private readonly IMapper _mapper;

    public InsightService(IUnitOfWork uow, ICurrentUser currentUser, IMapper mapper)
    {
        _uow = uow;
        _currentUser = currentUser;
        _mapper = mapper;
    }

    public async Task<ApiResponse<UserInsightDto>> GetMyInsightsAsync(CancellationToken ct = default)
    {
        return await GetUserInsightsAsync(_currentUser.UserId, ct);
    }

    public async Task<ApiResponse<UserInsightDto>> GetUserInsightsAsync(string userId, CancellationToken ct = default)
    {
        var cached = await _uow.Repository<UserInsight>().Query()
            .Include(i => i.User)
            .Where(i => i.UserId == userId)
            .OrderByDescending(i => i.PeriodEnd)
            .FirstOrDefaultAsync(ct);

        if (cached is not null && cached.PeriodEnd > DateTime.UtcNow.AddHours(-24))
        {
            var dto = _mapper.Map<UserInsightDto>(cached);
            dto.ActionItemCompletionRate = cached.TotalActionItemsAssigned > 0
                ? (double)cached.TotalActionItemsCompleted / cached.TotalActionItemsAssigned * 100
                : 0;
            return ApiResponse<UserInsightDto>.Ok(dto);
        }

        await RefreshInsightsAsync(userId, ct);

        var refreshed = await _uow.Repository<UserInsight>().Query()
            .Include(i => i.User)
            .Where(i => i.UserId == userId)
            .OrderByDescending(i => i.PeriodEnd)
            .FirstOrDefaultAsync(ct);

        if (refreshed is null)
            return ApiResponse<UserInsightDto>.Fail("No data available.");

        var result = _mapper.Map<UserInsightDto>(refreshed);
        result.ActionItemCompletionRate = refreshed.TotalActionItemsAssigned > 0
            ? (double)refreshed.TotalActionItemsCompleted / refreshed.TotalActionItemsAssigned * 100
            : 0;

        return ApiResponse<UserInsightDto>.Ok(result);
    }

    public async Task<ApiResponse<IEnumerable<GrowthSnapshotDto>>> GetGrowthSnapshotsAsync(int months, CancellationToken ct = default)
    {
        var since = DateTime.UtcNow.AddMonths(-months);

        var snapshots = await _uow.Repository<UserGrowthSnapshot>().Query()
            .Where(s => s.UserId == _currentUser.UserId && s.SnapshotDate >= since)
            .OrderBy(s => s.Year).ThenBy(s => s.Month)
            .ToListAsync(ct);

        if (snapshots.Count == 0)
        {
            await GenerateSnapshotsAsync(_currentUser.UserId, months, ct);
            snapshots = await _uow.Repository<UserGrowthSnapshot>().Query()
                .Where(s => s.UserId == _currentUser.UserId && s.SnapshotDate >= since)
                .OrderBy(s => s.Year).ThenBy(s => s.Month)
                .ToListAsync(ct);
        }

        var dtos = snapshots.Select(s =>
        {
            var dto = _mapper.Map<GrowthSnapshotDto>(s);
            dto.MonthLabel = new DateTime(s.Year, s.Month, 1).ToString("MMM yyyy", CultureInfo.InvariantCulture);
            return dto;
        });

        return ApiResponse<IEnumerable<GrowthSnapshotDto>>.Ok(dtos);
    }

    public async Task RefreshInsightsAsync(string userId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var periodStart = now.AddMonths(-6);

        var sessionsAttended = await _uow.Repository<SessionMember>().Query()
            .CountAsync(m => m.UserId == userId && m.JoinedAt >= periodStart, ct);

        var pointsSubmitted = await _uow.Repository<DiscussionPoint>().Query()
            .CountAsync(p => p.AuthorId == userId && p.CreatedAt >= periodStart, ct);

        var votesCast = await _uow.Repository<Vote>().Query()
            .CountAsync(v => v.UserId == userId && v.CreatedAt >= periodStart, ct);

        var actionItemsAssigned = await _uow.Repository<ActionItem>().Query()
            .CountAsync(a => a.AssignedToId == userId && a.CreatedAt >= periodStart, ct);

        var actionItemsCompleted = await _uow.Repository<ActionItem>().Query()
            .CountAsync(a => a.AssignedToId == userId && a.Status == ActionItemStatus.Done && a.CreatedAt >= periodStart, ct);

        var engagement = sessionsAttended * 10.0 + pointsSubmitted * 5.0 + votesCast * 2.0 + actionItemsCompleted * 8.0;
        var maxPossible = Math.Max(1, sessionsAttended * 10.0 + sessionsAttended * 15.0 + sessionsAttended * 6.0 + actionItemsAssigned * 8.0);
        var engagementScore = Math.Min(100, engagement / maxPossible * 100);

        var existing = await _uow.Repository<UserInsight>().FirstOrDefaultAsync(
            i => i.UserId == userId, ct);

        if (existing is not null)
        {
            existing.TotalSessionsAttended = sessionsAttended;
            existing.TotalPointsSubmitted = pointsSubmitted;
            existing.TotalVotesCast = votesCast;
            existing.TotalActionItemsAssigned = actionItemsAssigned;
            existing.TotalActionItemsCompleted = actionItemsCompleted;
            existing.AverageEngagementScore = Math.Round(engagementScore, 1);
            existing.PeriodStart = periodStart;
            existing.PeriodEnd = now;
            _uow.Repository<UserInsight>().Update(existing);
        }
        else
        {
            var insight = new UserInsight
            {
                UserId = userId,
                TotalSessionsAttended = sessionsAttended,
                TotalPointsSubmitted = pointsSubmitted,
                TotalVotesCast = votesCast,
                TotalActionItemsAssigned = actionItemsAssigned,
                TotalActionItemsCompleted = actionItemsCompleted,
                AverageEngagementScore = Math.Round(engagementScore, 1),
                PeriodStart = periodStart,
                PeriodEnd = now
            };
            await _uow.Repository<UserInsight>().AddAsync(insight, ct);
        }

        await _uow.SaveChangesAsync(ct);
    }

    public async Task<ApiResponse<UserInsightDto>> RefreshMyInsightsAsync(CancellationToken ct = default)
    {
        await RefreshInsightsAsync(_currentUser.UserId, ct);
        return await GetMyInsightsAsync(ct);
    }

    private async Task GenerateSnapshotsAsync(string userId, int months, CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        for (int i = months - 1; i >= 0; i--)
        {
            var date = now.AddMonths(-i);
            var monthStart = new DateTime(date.Year, date.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var monthEnd = monthStart.AddMonths(1);

            var exists = await _uow.Repository<UserGrowthSnapshot>().AnyAsync(
                s => s.UserId == userId && s.Year == monthStart.Year && s.Month == monthStart.Month, ct);

            if (exists) continue;

            var sessions = await _uow.Repository<SessionMember>().Query()
                .CountAsync(m => m.UserId == userId && m.JoinedAt >= monthStart && m.JoinedAt < monthEnd, ct);

            var points = await _uow.Repository<DiscussionPoint>().Query()
                .CountAsync(p => p.AuthorId == userId && p.CreatedAt >= monthStart && p.CreatedAt < monthEnd, ct);

            var completed = await _uow.Repository<ActionItem>().Query()
                .CountAsync(a => a.AssignedToId == userId && a.Status == ActionItemStatus.Done
                             && a.CompletedAt >= monthStart && a.CompletedAt < monthEnd, ct);

            var engagement = sessions * 10.0 + points * 5.0 + completed * 8.0;

            var snapshot = new UserGrowthSnapshot
            {
                UserId = userId,
                Month = monthStart.Month,
                Year = monthStart.Year,
                SessionsAttended = sessions,
                PointsSubmitted = points,
                ActionItemsCompleted = completed,
                EngagementScore = Math.Round(Math.Min(100, engagement), 1),
                SnapshotDate = monthStart
            };

            await _uow.Repository<UserGrowthSnapshot>().AddAsync(snapshot, ct);
        }

        await _uow.SaveChangesAsync(ct);
    }
}
