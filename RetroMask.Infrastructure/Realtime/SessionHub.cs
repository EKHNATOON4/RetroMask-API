using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace RetroMask.Infrastructure.Realtime;

[Authorize]
public class SessionHub : Hub
{
    private readonly ILogger<SessionHub> _logger;

    public SessionHub(ILogger<SessionHub> logger)
    {
        _logger = logger;
    }

    public async Task JoinSession(string sessionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"session:{sessionId}");
        _logger.LogInformation("User {UserId} joined session {SessionId}", Context.UserIdentifier, sessionId);
        await Clients.Group($"session:{sessionId}").SendAsync("UserJoined", Context.UserIdentifier);
    }

    public async Task LeaveSession(string sessionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"session:{sessionId}");
        await Clients.Group($"session:{sessionId}").SendAsync("UserLeft", Context.UserIdentifier);
    }

    public async Task SendTypingIndicator(string sessionId, bool isTyping)
    {
        await Clients.OthersInGroup($"session:{sessionId}").SendAsync("TypingIndicator", Context.UserIdentifier, isTyping);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("User {UserId} disconnected", Context.UserIdentifier);
        await base.OnDisconnectedAsync(exception);
    }
}
