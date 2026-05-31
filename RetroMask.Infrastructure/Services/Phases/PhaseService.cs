using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RetroMask.Application.Abstractions;
using RetroMask.Application.Abstractions.Repositories;
using RetroMask.Application.Common;
using RetroMask.Application.Dtos.Sessions;
using RetroMask.Application.Services.Phases;
using RetroMask.Domain.Entities.Sessions;
using RetroMask.Domain.Enums;

namespace RetroMask.Infrastructure.Services.Phases;

public class PhaseService : IPhaseService
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    private readonly IMapper _mapper;
    private readonly ISessionBroadcaster _broadcaster;

    public PhaseService(
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

    public async Task<ApiResponse<PhaseDto>> GetPhaseByIdAsync(Guid phaseId, CancellationToken ct = default)
    {
        var phase = await _uow.Repository<SessionPhase>().Query()
            .Include(p => p.DiscussionPoints)
            .FirstOrDefaultAsync(p => p.Id == phaseId, ct);

        if (phase is null)
            return ApiResponse<PhaseDto>.Fail("Phase not found.");

        return ApiResponse<PhaseDto>.Ok(_mapper.Map<PhaseDto>(phase));
    }

    public async Task<ApiResponse<IEnumerable<PhaseDto>>> GetSessionPhasesAsync(Guid sessionId, CancellationToken ct = default)
    {
        var phases = await _uow.Repository<SessionPhase>().Query()
            .Where(p => p.SessionId == sessionId)
            .Include(p => p.DiscussionPoints)
            .OrderBy(p => p.Order)
            .ToListAsync(ct);

        return ApiResponse<IEnumerable<PhaseDto>>.Ok(_mapper.Map<IEnumerable<PhaseDto>>(phases));
    }

    public async Task<ApiResponse<PhaseDto>> ActivatePhaseAsync(Guid phaseId, CancellationToken ct = default)
    {
        var phase = await _uow.Repository<SessionPhase>().Query()
            .Include(p => p.Session)
            .Include(p => p.DiscussionPoints)
            .FirstOrDefaultAsync(p => p.Id == phaseId, ct);

        if (phase is null)
            return ApiResponse<PhaseDto>.Fail("Phase not found.");

        if (phase.Session is null)
            return ApiResponse<PhaseDto>.Fail("Session context not found.");

        if (phase.Session.FacilitatorId != _currentUser.UserId)
            return ApiResponse<PhaseDto>.Fail("Only the facilitator can activate phases.");

        if (phase.Session.Status != SessionStatus.Active)
            return ApiResponse<PhaseDto>.Fail("Session must be active to activate a phase.");

        if (phase.Status != PhaseStatus.Pending)
            return ApiResponse<PhaseDto>.Fail("Only pending phases can be activated.");

        var activePhases = await _uow.Repository<SessionPhase>().Query()
            .Where(p => p.SessionId == phase.SessionId && p.Status == PhaseStatus.Active)
            .ToListAsync(ct);

        foreach (var active in activePhases)
        {
            active.Status = PhaseStatus.Completed;
            active.CompletedAt = DateTime.UtcNow;
            _uow.Repository<SessionPhase>().Update(active);
        }

        phase.Status = PhaseStatus.Active;
        phase.StartedAt = DateTime.UtcNow;
        _uow.Repository<SessionPhase>().Update(phase);
        await _uow.SaveChangesAsync(ct);

        await _broadcaster.NotifyPhaseChangedAsync(phase.SessionId, phase.Id, ct);

        return ApiResponse<PhaseDto>.Ok(_mapper.Map<PhaseDto>(phase));
    }

    public async Task<ApiResponse<PhaseDto>> CompletePhaseAsync(Guid phaseId, CancellationToken ct = default)
    {
        var phase = await _uow.Repository<SessionPhase>().Query()
            .Include(p => p.Session)
            .Include(p => p.DiscussionPoints)
            .FirstOrDefaultAsync(p => p.Id == phaseId, ct);

        if (phase is null)
            return ApiResponse<PhaseDto>.Fail("Phase not found.");

        if (phase.Session is null)
            return ApiResponse<PhaseDto>.Fail("Session context not found.");

        if (phase.Session.FacilitatorId != _currentUser.UserId)
            return ApiResponse<PhaseDto>.Fail("Only the facilitator can complete phases.");

        if (phase.Status != PhaseStatus.Active)
            return ApiResponse<PhaseDto>.Fail("Only active phases can be completed.");

        phase.Status = PhaseStatus.Completed;
        phase.CompletedAt = DateTime.UtcNow;
        _uow.Repository<SessionPhase>().Update(phase);
        await _uow.SaveChangesAsync(ct);

        await _broadcaster.NotifyPhaseChangedAsync(phase.SessionId, phase.Id, ct);

        return ApiResponse<PhaseDto>.Ok(_mapper.Map<PhaseDto>(phase));
    }

    public async Task<ApiResponse<PhaseDto>> SkipPhaseAsync(Guid phaseId, CancellationToken ct = default)
    {
        var phase = await _uow.Repository<SessionPhase>().Query()
            .Include(p => p.Session)
            .Include(p => p.DiscussionPoints)
            .FirstOrDefaultAsync(p => p.Id == phaseId, ct);

        if (phase is null)
            return ApiResponse<PhaseDto>.Fail("Phase not found.");

        if (phase.Session is null)
            return ApiResponse<PhaseDto>.Fail("Session context not found.");

        if (phase.Session.FacilitatorId != _currentUser.UserId)
            return ApiResponse<PhaseDto>.Fail("Only the facilitator can skip phases.");

        if (phase.Status == PhaseStatus.Completed)
            return ApiResponse<PhaseDto>.Fail("Cannot skip a completed phase.");

        phase.Status = PhaseStatus.Skipped;
        phase.CompletedAt = DateTime.UtcNow;
        _uow.Repository<SessionPhase>().Update(phase);
        await _uow.SaveChangesAsync(ct);

        await _broadcaster.NotifyPhaseChangedAsync(phase.SessionId, phase.Id, ct);

        return ApiResponse<PhaseDto>.Ok(_mapper.Map<PhaseDto>(phase));
    }

    public async Task<ApiResponse<PhaseDto>> ExtendPhaseAsync(Guid phaseId, ExtendPhaseRequest request, CancellationToken ct = default)
    {
        var phase = await _uow.Repository<SessionPhase>().Query()
            .Include(p => p.Session)
            .Include(p => p.DiscussionPoints)
            .FirstOrDefaultAsync(p => p.Id == phaseId, ct);

        if (phase is null)
            return ApiResponse<PhaseDto>.Fail("Phase not found.");

        if (phase.Session is null)
            return ApiResponse<PhaseDto>.Fail("Session context not found.");

        if (phase.Session.FacilitatorId != _currentUser.UserId)
            return ApiResponse<PhaseDto>.Fail("Only the facilitator can extend phases.");

        if (phase.Status != PhaseStatus.Active)
            return ApiResponse<PhaseDto>.Fail("Only active phases can be extended.");

        if (request.AdditionalMinutes < 1 || request.AdditionalMinutes > 60)
            return ApiResponse<PhaseDto>.Fail("Additional minutes must be between 1 and 60.");

        phase.DurationMinutes = (phase.DurationMinutes ?? 0) + request.AdditionalMinutes;
        _uow.Repository<SessionPhase>().Update(phase);
        await _uow.SaveChangesAsync(ct);

        await _broadcaster.BroadcastToSessionAsync(phase.SessionId, "PhaseExtended",
            new { PhaseId = phase.Id, phase.DurationMinutes, AddedMinutes = request.AdditionalMinutes }, ct);

        return ApiResponse<PhaseDto>.Ok(_mapper.Map<PhaseDto>(phase));
    }

    public async Task<ApiResponse<PhaseDto>> AdvanceToNextPhaseAsync(Guid sessionId, CancellationToken ct = default)
    {
        var session = await _uow.Repository<Session>().GetByIdAsync(sessionId, ct);
        if (session is null)
            return ApiResponse<PhaseDto>.Fail("Session not found.");

        if (session.FacilitatorId != _currentUser.UserId)
            return ApiResponse<PhaseDto>.Fail("Only the facilitator can advance phases.");

        if (session.Status != SessionStatus.Active)
            return ApiResponse<PhaseDto>.Fail("Session must be active to advance phases.");

        var phases = await _uow.Repository<SessionPhase>().Query()
            .Where(p => p.SessionId == sessionId)
            .Include(p => p.DiscussionPoints)
            .OrderBy(p => p.Order)
            .ToListAsync(ct);

        var currentPhase = phases.FirstOrDefault(p => p.Status == PhaseStatus.Active);
        if (currentPhase is not null)
        {
            currentPhase.Status = PhaseStatus.Completed;
            currentPhase.CompletedAt = DateTime.UtcNow;
            _uow.Repository<SessionPhase>().Update(currentPhase);
        }

        var nextPhase = phases.FirstOrDefault(p => p.Status == PhaseStatus.Pending);
        if (nextPhase is null)
        {
            session.Status = SessionStatus.Completed;
            session.CompletedAt = DateTime.UtcNow;
            _uow.Repository<Session>().Update(session);
            await _uow.SaveChangesAsync(ct);

            await _broadcaster.BroadcastToSessionAsync(sessionId, "SessionCompleted",
                new { SessionId = sessionId }, ct);

            if (currentPhase is not null)
                return ApiResponse<PhaseDto>.Ok(_mapper.Map<PhaseDto>(currentPhase), "All phases completed. Session ended.");

            return ApiResponse<PhaseDto>.Fail("No phases to advance.");
        }

        nextPhase.Status = PhaseStatus.Active;
        nextPhase.StartedAt = DateTime.UtcNow;
        _uow.Repository<SessionPhase>().Update(nextPhase);
        await _uow.SaveChangesAsync(ct);

        await _broadcaster.NotifyPhaseChangedAsync(sessionId, nextPhase.Id, ct);

        return ApiResponse<PhaseDto>.Ok(_mapper.Map<PhaseDto>(nextPhase));
    }

    public async Task<ApiResponse<PhaseDto>> ReorderPhasesAsync(
        Guid sessionId, ReorderPhasesRequest request, CancellationToken ct = default)
    {
        var session = await _uow.Repository<Session>().GetByIdAsync(sessionId, ct);
        if (session is null)
            return ApiResponse<PhaseDto>.Fail("Session not found.");

        if (session.FacilitatorId != _currentUser.UserId)
            return ApiResponse<PhaseDto>.Fail("Only the facilitator can reorder phases.");

        if (session.Status != SessionStatus.Draft)
            return ApiResponse<PhaseDto>.Fail("Phases can only be reordered in draft sessions.");

        var phases = await _uow.Repository<SessionPhase>().Query()
            .Where(p => p.SessionId == sessionId)
            .ToListAsync(ct);

        foreach (var item in request.Order)
        {
            var phase = phases.FirstOrDefault(p => p.Id == item.PhaseId);
            if (phase is not null)
            {
                phase.Order = item.Order;
                _uow.Repository<SessionPhase>().Update(phase);
            }
        }

        await _uow.SaveChangesAsync(ct);

        var reorderedPhases = phases.OrderBy(p => p.Order).ToList();
        if (reorderedPhases.Count == 0)
            return ApiResponse<PhaseDto>.Fail("No phases found for this session.");

        return ApiResponse<PhaseDto>.Ok(_mapper.Map<PhaseDto>(reorderedPhases.First()), "Phases reordered successfully.");
    }
}
