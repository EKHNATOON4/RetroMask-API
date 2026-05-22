using Microsoft.AspNetCore.SignalR;
using RetroMask.Application.Abstractions;

namespace RetroMask.Infrastructure.Realtime;

public class SessionBroadcaster : ISessionBroadcaster
{
    private readonly IHubContext<SessionHub> _hub;

    public SessionBroadcaster(IHubContext<SessionHub> hub)
    {
        _hub = hub;
    }

    public async Task BroadcastToSessionAsync(Guid sessionId, string eventName, object payload, CancellationToken ct = default)
        => await _hub.Clients.Group($"session:{sessionId}").SendAsync(eventName, payload, ct);

    public async Task BroadcastToUserAsync(string userId, string eventName, object payload, CancellationToken ct = default)
        => await _hub.Clients.User(userId).SendAsync(eventName, payload, ct);

    public async Task NotifyPhaseChangedAsync(Guid sessionId, Guid phaseId, CancellationToken ct = default)
        => await BroadcastToSessionAsync(sessionId, "PhaseChanged", new { SessionId = sessionId, PhaseId = phaseId }, ct);

    public async Task NotifyPointAddedAsync(Guid sessionId, object pointDto, CancellationToken ct = default)
        => await BroadcastToSessionAsync(sessionId, "PointAdded", pointDto, ct);

    public async Task NotifyVoteUpdatedAsync(Guid sessionId, Guid pointId, CancellationToken ct = default)
        => await BroadcastToSessionAsync(sessionId, "VoteUpdated", new { SessionId = sessionId, PointId = pointId }, ct);
}
