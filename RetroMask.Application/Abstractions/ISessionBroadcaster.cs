namespace RetroMask.Application.Abstractions;

public interface ISessionBroadcaster
{
    Task BroadcastToSessionAsync(Guid sessionId, string eventName, object payload, CancellationToken ct = default);
    Task BroadcastToUserAsync(string userId, string eventName, object payload, CancellationToken ct = default);
    Task NotifyPhaseChangedAsync(Guid sessionId, Guid phaseId, CancellationToken ct = default);
    Task NotifyPointAddedAsync(Guid sessionId, object pointDto, CancellationToken ct = default);
    Task NotifyVoteUpdatedAsync(Guid sessionId, Guid pointId, CancellationToken ct = default);
}
