using System.Security.Cryptography;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RetroMask.Application.Abstractions;
using RetroMask.Application.Abstractions.Repositories;
using RetroMask.Application.Common;
using RetroMask.Application.Common.Exceptions;
using RetroMask.Application.Dtos.Sessions;
using RetroMask.Application.Services.Sessions;
using RetroMask.Domain.Entities.Sessions;
using RetroMask.Domain.Entities.Teams;
using RetroMask.Domain.Enums;

namespace RetroMask.Infrastructure.Services.Sessions;

public class SessionService : ISessionService
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    private readonly IMapper _mapper;
    private readonly ISessionBroadcaster _broadcaster;

    private static readonly string[] MaskNames =
    {
        "Phoenix", "Shadow", "Echo", "Prism", "Cipher",
        "Nimbus", "Raven", "Spark", "Drift", "Frost",
        "Blaze", "Mist", "Comet", "Pulse", "Flare",
        "Sage", "Storm", "Dusk", "Reef", "Orbit",
        "Luna", "Cobalt", "Ember", "Nova", "Vortex"
    };

    public SessionService(
        IUnitOfWork uow,
        ICurrentUser currentUser,
        IMapper mapper,
        ISessionBroadcaster broadcaster)
    {
        _uow = uow;
        _currentUser = currentUser;
        _mapper = mapper;
        _broadcaster = broadcaster;
    }

    public async Task<ApiResponse<SessionDto>> CreateSessionAsync(CreateSessionRequest request, CancellationToken ct = default)
    {
        var team = await _uow.Repository<Team>().Query()
            .Include(t => t.Members)
            .FirstOrDefaultAsync(t => t.Id == request.TeamId, ct);

        if (team is null)
            return ApiResponse<SessionDto>.Fail("Team not found.");

        var isMember = team.Members.Any(m => m.UserId == _currentUser.UserId && m.IsActive);
        if (!isMember)
            return ApiResponse<SessionDto>.Fail("You are not a member of this team.");

        var session = new Session
        {
            Title = request.Title,
            Description = request.Description,
            TeamId = request.TeamId,
            FacilitatorId = _currentUser.UserId,
            TemplateId = request.TemplateId,
            IsAnonymous = request.IsAnonymous,
            VotingEnabled = request.VotingEnabled,
            MaxVotesPerUser = request.MaxVotesPerUser,
            ScheduledAt = request.ScheduledAt,
            CreatedBy = _currentUser.UserId
        };

        await _uow.Repository<Session>().AddAsync(session, ct);

        if (request.TemplateId.HasValue)
        {
            var template = await _uow.Repository<SessionTemplate>().GetByIdAsync(request.TemplateId.Value, ct);
            if (template is not null)
                await CreatePhasesFromTemplate(session.Id, template, ct);
        }
        else
        {
            await CreateDefaultPhases(session.Id, ct);
        }

        await _uow.SaveChangesAsync(ct);

        return await GetSessionByIdAsync(session.Id, ct);
    }

    public async Task<ApiResponse<SessionDto>> GetSessionByIdAsync(Guid sessionId, CancellationToken ct = default)
    {
        var session = await _uow.Repository<Session>().Query()
            .Include(s => s.Team)
            .Include(s => s.Facilitator)
            .Include(s => s.Members)
            .Include(s => s.Phases).ThenInclude(p => p.DiscussionPoints)
            .FirstOrDefaultAsync(s => s.Id == sessionId, ct);

        if (session is null)
            return ApiResponse<SessionDto>.Fail("Session not found.");

        var dto = _mapper.Map<SessionDto>(session);
        return ApiResponse<SessionDto>.Ok(dto);
    }

    public async Task<ApiResponse<PagedResult<SessionSummaryDto>>> GetTeamSessionsAsync(
        Guid teamId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _uow.Repository<Session>().Query()
            .Where(s => s.TeamId == teamId)
            .OrderByDescending(s => s.CreatedAt);

        var total = await query.CountAsync(ct);
        var sessions = await query
            .Include(s => s.Members)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var dtos = _mapper.Map<IEnumerable<SessionSummaryDto>>(sessions);
        var result = PagedResult<SessionSummaryDto>.Create(dtos, total, page, pageSize);
        return ApiResponse<PagedResult<SessionSummaryDto>>.Ok(result);
    }

    public async Task<ApiResponse<SessionDto>> UpdateSessionAsync(
        Guid sessionId, UpdateSessionRequest request, CancellationToken ct = default)
    {
        var session = await _uow.Repository<Session>().GetByIdAsync(sessionId, ct);
        if (session is null)
            return ApiResponse<SessionDto>.Fail("Session not found.");

        if (session.FacilitatorId != _currentUser.UserId)
            return ApiResponse<SessionDto>.Fail("Only the facilitator can update the session.");

        if (session.Status != SessionStatus.Draft)
            return ApiResponse<SessionDto>.Fail("Only draft sessions can be updated.");

        if (request.Title is not null) session.Title = request.Title;
        if (request.Description is not null) session.Description = request.Description;
        if (request.IsAnonymous.HasValue) session.IsAnonymous = request.IsAnonymous.Value;
        if (request.VotingEnabled.HasValue) session.VotingEnabled = request.VotingEnabled.Value;
        if (request.MaxVotesPerUser.HasValue) session.MaxVotesPerUser = request.MaxVotesPerUser.Value;

        _uow.Repository<Session>().Update(session);
        await _uow.SaveChangesAsync(ct);

        return await GetSessionByIdAsync(sessionId, ct);
    }

    public async Task<ApiResponse<SessionDto>> StartSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        var session = await _uow.Repository<Session>().Query()
            .Include(s => s.Phases.OrderBy(p => p.Order))
            .FirstOrDefaultAsync(s => s.Id == sessionId, ct);

        if (session is null)
            return ApiResponse<SessionDto>.Fail("Session not found.");

        if (session.FacilitatorId != _currentUser.UserId)
            return ApiResponse<SessionDto>.Fail("Only the facilitator can start the session.");

        if (session.Status != SessionStatus.Draft && session.Status != SessionStatus.Paused)
            return ApiResponse<SessionDto>.Fail("Session cannot be started from its current state.");

        session.Status = SessionStatus.Active;
        session.StartedAt ??= DateTime.UtcNow;

        var firstPhase = session.Phases.FirstOrDefault(p => p.Status == PhaseStatus.Pending);
        if (firstPhase is not null && session.Phases.All(p => p.Status == PhaseStatus.Pending))
        {
            firstPhase.Status = PhaseStatus.Active;
            firstPhase.StartedAt = DateTime.UtcNow;
        }

        _uow.Repository<Session>().Update(session);
        await _uow.SaveChangesAsync(ct);

        await _broadcaster.BroadcastToSessionAsync(sessionId, "SessionStarted",
            new { SessionId = sessionId }, ct);

        if (firstPhase is not null && firstPhase.Status == PhaseStatus.Active)
            await _broadcaster.NotifyPhaseChangedAsync(sessionId, firstPhase.Id, ct);

        return await GetSessionByIdAsync(sessionId, ct);
    }

    public async Task<ApiResponse<SessionDto>> PauseSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        var session = await _uow.Repository<Session>().GetByIdAsync(sessionId, ct);
        if (session is null)
            return ApiResponse<SessionDto>.Fail("Session not found.");

        if (session.FacilitatorId != _currentUser.UserId)
            return ApiResponse<SessionDto>.Fail("Only the facilitator can pause the session.");

        if (session.Status != SessionStatus.Active)
            return ApiResponse<SessionDto>.Fail("Only active sessions can be paused.");

        session.Status = SessionStatus.Paused;
        _uow.Repository<Session>().Update(session);
        await _uow.SaveChangesAsync(ct);

        await _broadcaster.BroadcastToSessionAsync(sessionId, "SessionPaused",
            new { SessionId = sessionId }, ct);

        return await GetSessionByIdAsync(sessionId, ct);
    }

    public async Task<ApiResponse<SessionDto>> CompleteSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        var session = await _uow.Repository<Session>().Query()
            .Include(s => s.Phases)
            .FirstOrDefaultAsync(s => s.Id == sessionId, ct);

        if (session is null)
            return ApiResponse<SessionDto>.Fail("Session not found.");

        if (session.FacilitatorId != _currentUser.UserId)
            return ApiResponse<SessionDto>.Fail("Only the facilitator can complete the session.");

        if (session.Status != SessionStatus.Active && session.Status != SessionStatus.Paused)
            return ApiResponse<SessionDto>.Fail("Session cannot be completed from its current state.");

        session.Status = SessionStatus.Completed;
        session.CompletedAt = DateTime.UtcNow;

        foreach (var phase in session.Phases.Where(p => p.Status == PhaseStatus.Active))
        {
            phase.Status = PhaseStatus.Completed;
            phase.CompletedAt = DateTime.UtcNow;
        }

        _uow.Repository<Session>().Update(session);
        await _uow.SaveChangesAsync(ct);

        await _broadcaster.BroadcastToSessionAsync(sessionId, "SessionCompleted",
            new { SessionId = sessionId }, ct);

        return await GetSessionByIdAsync(sessionId, ct);
    }

    public async Task<ApiResponse> DeleteSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        var session = await _uow.Repository<Session>().GetByIdAsync(sessionId, ct);
        if (session is null)
            return ApiResponse.Fail("Session not found.");

        if (session.FacilitatorId != _currentUser.UserId)
            return ApiResponse.Fail("Only the facilitator can delete the session.");

        session.IsDeleted = true;
        session.DeletedAt = DateTime.UtcNow;
        session.DeletedBy = _currentUser.UserId;

        _uow.Repository<Session>().Update(session);
        await _uow.SaveChangesAsync(ct);

        return ApiResponse.Ok("Session deleted successfully.");
    }

    public async Task<ApiResponse> JoinSessionAsync(Guid sessionId, JoinSessionRequest request, CancellationToken ct = default)
    {
        var session = await _uow.Repository<Session>().Query()
            .Include(s => s.Members)
            .Include(s => s.Team).ThenInclude(t => t.Members)
            .FirstOrDefaultAsync(s => s.Id == sessionId, ct);

        if (session is null)
            return ApiResponse.Fail("Session not found.");

        if (session.Status == SessionStatus.Completed || session.Status == SessionStatus.Cancelled)
            return ApiResponse.Fail("Cannot join a completed or cancelled session.");

        var isTeamMember = session.Team.Members.Any(m => m.UserId == _currentUser.UserId && m.IsActive);
        if (!isTeamMember)
            return ApiResponse.Fail("You must be a team member to join this session.");

        var existing = session.Members.FirstOrDefault(m => m.UserId == _currentUser.UserId);
        if (existing is not null)
        {
            if (existing.LeftAt is null)
                return ApiResponse.Fail("You are already in this session.");

            existing.LeftAt = null;
            existing.IsOnline = true;
            _uow.Repository<SessionMember>().Update(existing);
        }
        else
        {
            var maskName = request.MaskName;
            if (session.IsAnonymous && string.IsNullOrWhiteSpace(maskName))
                maskName = AssignMaskName(session.Members);

            var member = new SessionMember
            {
                SessionId = sessionId,
                UserId = _currentUser.UserId,
                MaskName = maskName,
                IsOnline = true,
                JoinedAt = DateTime.UtcNow
            };

            await _uow.Repository<SessionMember>().AddAsync(member, ct);
        }

        await _uow.SaveChangesAsync(ct);

        await _broadcaster.BroadcastToSessionAsync(sessionId, "MemberJoined",
            new { SessionId = sessionId, UserId = _currentUser.UserId }, ct);

        return ApiResponse.Ok("Joined session successfully.");
    }

    public async Task<ApiResponse> LeaveSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        var member = await _uow.Repository<SessionMember>().FirstOrDefaultAsync(
            m => m.SessionId == sessionId && m.UserId == _currentUser.UserId && m.LeftAt == null, ct);

        if (member is null)
            return ApiResponse.Fail("You are not in this session.");

        member.LeftAt = DateTime.UtcNow;
        member.IsOnline = false;
        _uow.Repository<SessionMember>().Update(member);
        await _uow.SaveChangesAsync(ct);

        await _broadcaster.BroadcastToSessionAsync(sessionId, "MemberLeft",
            new { SessionId = sessionId, UserId = _currentUser.UserId }, ct);

        return ApiResponse.Ok("Left session successfully.");
    }

    private static string AssignMaskName(ICollection<SessionMember> existingMembers)
    {
        var usedNames = existingMembers
            .Where(m => m.MaskName is not null)
            .Select(m => m.MaskName)
            .ToHashSet();

        var available = MaskNames.Where(n => !usedNames.Contains(n)).ToList();
        if (available.Count == 0)
            return $"Mask-{RandomNumberGenerator.GetInt32(1000, 9999)}";

        return available[RandomNumberGenerator.GetInt32(available.Count)];
    }

    private async Task CreateDefaultPhases(Guid sessionId, CancellationToken ct)
    {
        var defaults = new (SessionPhaseType type, string title, int duration)[]
        {
            (SessionPhaseType.Icebreaker, "Icebreaker", 5),
            (SessionPhaseType.WentWell, "What went well?", 10),
            (SessionPhaseType.ToImprove, "What to improve?", 10),
            (SessionPhaseType.ActionItems, "Action Items", 10),
            (SessionPhaseType.Shoutouts, "Shoutouts", 5)
        };

        var phases = defaults.Select((d, i) => new SessionPhase
        {
            SessionId = sessionId,
            Title = d.title,
            PhaseType = d.type,
            Order = i,
            DurationMinutes = d.duration,
            CreatedBy = _currentUser.UserId
        });

        await _uow.Repository<SessionPhase>().AddRangeAsync(phases, ct);
    }

    private async Task CreatePhasesFromTemplate(Guid sessionId, SessionTemplate template, CancellationToken ct)
    {
        try
        {
            var phaseConfigs = System.Text.Json.JsonSerializer.Deserialize<List<PhaseTemplateConfig>>(
                template.PhasesJson,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (phaseConfigs is null || phaseConfigs.Count == 0)
            {
                await CreateDefaultPhases(sessionId, ct);
                return;
            }

            var phases = phaseConfigs.Select((p, i) => new SessionPhase
            {
                SessionId = sessionId,
                Title = p.Title ?? p.PhaseType.ToString(),
                Description = p.Description,
                PhaseType = p.PhaseType,
                Order = i,
                DurationMinutes = p.DurationMinutes,
                CreatedBy = _currentUser.UserId
            });

            await _uow.Repository<SessionPhase>().AddRangeAsync(phases, ct);
        }
        catch
        {
            await CreateDefaultPhases(sessionId, ct);
        }
    }

    private class PhaseTemplateConfig
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public SessionPhaseType PhaseType { get; set; }
        public int? DurationMinutes { get; set; }
    }
}
