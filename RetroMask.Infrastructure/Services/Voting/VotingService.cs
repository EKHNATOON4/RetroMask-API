using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RetroMask.Application.Abstractions;
using RetroMask.Application.Abstractions.Repositories;
using RetroMask.Application.Common;
using RetroMask.Application.Dtos.Voting;
using RetroMask.Application.Services.Voting;
using RetroMask.Domain.Entities.Discussion;
using RetroMask.Domain.Entities.Sessions;
using RetroMask.Domain.Entities.Voting;
using RetroMask.Domain.Enums;

namespace RetroMask.Infrastructure.Services.Voting;

public class VotingService : IVotingService
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    private readonly ISessionBroadcaster _broadcaster;

    public VotingService(IUnitOfWork uow, ICurrentUser currentUser, ISessionBroadcaster broadcaster)
    {
        _uow = uow;
        _currentUser = currentUser;
        _broadcaster = broadcaster;
    }

    public async Task<ApiResponse<VoteResultDto>> CastVoteAsync(Guid pointId, CastVoteRequest request, CancellationToken ct = default)
    {
        var point = await _uow.Repository<DiscussionPoint>().Query()
            .Include(p => p.Phase).ThenInclude(ph => ph.Session)
            .Include(p => p.VoteSummary)
            .FirstOrDefaultAsync(p => p.Id == pointId, ct);

        if (point is null)
            return ApiResponse<VoteResultDto>.Fail("Point not found.");

        if (point.Phase?.Session is null)
            return ApiResponse<VoteResultDto>.Fail("Session context not found for this point.");

        if (!point.Phase.Session.VotingEnabled)
            return ApiResponse<VoteResultDto>.Fail("Voting is disabled for this session.");

        if (point.VoteSummary?.Status == VoteSummaryStatus.Closed)
            return ApiResponse<VoteResultDto>.Fail("Voting is closed for this point.");

        var sessionId = point.Phase.SessionId;
        var maxVotes = point.Phase.Session.MaxVotesPerUser;

        var userVoteCount = await _uow.Repository<Vote>().Query()
            .Where(v => v.UserId == _currentUser.UserId)
            .Where(v => v.DiscussionPoint.Phase.SessionId == sessionId)
            .CountAsync(ct);

        var existing = await _uow.Repository<Vote>().FirstOrDefaultAsync(
            v => v.DiscussionPointId == pointId && v.UserId == _currentUser.UserId, ct);

        if (existing is not null)
        {
            if (existing.VoteType == request.VoteType)
                return ApiResponse<VoteResultDto>.Fail("You already cast this vote.");

            existing.VoteType = request.VoteType;
            _uow.Repository<Vote>().Update(existing);
        }
        else
        {
            if (userVoteCount >= maxVotes)
                return ApiResponse<VoteResultDto>.Fail($"Vote budget exhausted. Max {maxVotes} votes per user.");

            var vote = new Vote
            {
                DiscussionPointId = pointId,
                UserId = _currentUser.UserId,
                VoteType = request.VoteType,
                CreatedBy = _currentUser.UserId
            };
            await _uow.Repository<Vote>().AddAsync(vote, ct);
        }

        await _uow.SaveChangesAsync(ct);
        await RecalculateSummaryAsync(pointId, ct);

        await _broadcaster.NotifyVoteUpdatedAsync(sessionId, pointId, ct);

        return await GetVoteSummaryAsync(pointId, ct);
    }

    public async Task<ApiResponse> RemoveVoteAsync(Guid pointId, CancellationToken ct = default)
    {
        var vote = await _uow.Repository<Vote>().FirstOrDefaultAsync(
            v => v.DiscussionPointId == pointId && v.UserId == _currentUser.UserId, ct);

        if (vote is null)
            return ApiResponse.Fail("No vote to remove.");

        _uow.Repository<Vote>().Remove(vote);
        await _uow.SaveChangesAsync(ct);
        await RecalculateSummaryAsync(pointId, ct);

        return ApiResponse.Ok("Vote removed.");
    }

    public async Task<ApiResponse<VoteResultDto>> GetVoteSummaryAsync(Guid pointId, CancellationToken ct = default)
    {
        var summary = await _uow.Repository<VoteSummary>().FirstOrDefaultAsync(
            s => s.DiscussionPointId == pointId, ct);

        var myVote = await _uow.Repository<Vote>().FirstOrDefaultAsync(
            v => v.DiscussionPointId == pointId && v.UserId == _currentUser.UserId, ct);

        var dto = new VoteResultDto
        {
            DiscussionPointId = pointId,
            UpVotes = summary?.UpVotes ?? 0,
            DownVotes = summary?.DownVotes ?? 0,
            Score = summary?.Score ?? 0,
            Status = summary?.Status ?? VoteSummaryStatus.Open,
            MyVote = myVote?.VoteType
        };

        return ApiResponse<VoteResultDto>.Ok(dto);
    }

    public async Task<ApiResponse<IEnumerable<VoteResultDto>>> GetSessionVoteSummariesAsync(Guid sessionId, CancellationToken ct = default)
    {
        var pointIds = await _uow.Repository<DiscussionPoint>().Query()
            .Where(p => p.Phase.SessionId == sessionId)
            .Select(p => p.Id)
            .ToListAsync(ct);

        var summaries = await _uow.Repository<VoteSummary>().Query()
            .Where(s => pointIds.Contains(s.DiscussionPointId))
            .ToListAsync(ct);

        var myVotes = await _uow.Repository<Vote>().Query()
            .Where(v => pointIds.Contains(v.DiscussionPointId) && v.UserId == _currentUser.UserId)
            .ToListAsync(ct);

        var dtos = pointIds.Select(pid =>
        {
            var s = summaries.FirstOrDefault(x => x.DiscussionPointId == pid);
            var mv = myVotes.FirstOrDefault(x => x.DiscussionPointId == pid);
            return new VoteResultDto
            {
                DiscussionPointId = pid,
                UpVotes = s?.UpVotes ?? 0,
                DownVotes = s?.DownVotes ?? 0,
                Score = s?.Score ?? 0,
                Status = s?.Status ?? VoteSummaryStatus.Open,
                MyVote = mv?.VoteType
            };
        });

        return ApiResponse<IEnumerable<VoteResultDto>>.Ok(dtos);
    }

    public async Task<ApiResponse> CloseVotingAsync(Guid pointId, CancellationToken ct = default)
    {
        var summary = await _uow.Repository<VoteSummary>().FirstOrDefaultAsync(
            s => s.DiscussionPointId == pointId, ct);

        if (summary is null)
        {
            summary = new VoteSummary
            {
                DiscussionPointId = pointId,
                Status = VoteSummaryStatus.Closed,
                ClosedAt = DateTime.UtcNow
            };
            await _uow.Repository<VoteSummary>().AddAsync(summary, ct);
        }
        else
        {
            summary.Status = VoteSummaryStatus.Closed;
            summary.ClosedAt = DateTime.UtcNow;
            _uow.Repository<VoteSummary>().Update(summary);
        }

        await _uow.SaveChangesAsync(ct);

        return ApiResponse.Ok("Voting closed.");
    }

    private async Task RecalculateSummaryAsync(Guid pointId, CancellationToken ct)
    {
        var votes = await _uow.Repository<Vote>().Query()
            .Where(v => v.DiscussionPointId == pointId)
            .ToListAsync(ct);

        var summary = await _uow.Repository<VoteSummary>().FirstOrDefaultAsync(
            s => s.DiscussionPointId == pointId, ct);

        var up = votes.Count(v => v.VoteType == VoteType.Up);
        var down = votes.Count(v => v.VoteType == VoteType.Down);

        if (summary is null)
        {
            summary = new VoteSummary
            {
                DiscussionPointId = pointId,
                UpVotes = up,
                DownVotes = down
            };
            await _uow.Repository<VoteSummary>().AddAsync(summary, ct);
        }
        else
        {
            summary.UpVotes = up;
            summary.DownVotes = down;
            _uow.Repository<VoteSummary>().Update(summary);
        }

        await _uow.SaveChangesAsync(ct);
    }
}
