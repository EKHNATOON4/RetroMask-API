namespace RetroMask.Application.Abstractions;

public interface ICurrentUser
{
    string UserId { get; }
    string Email { get; }
    string? DisplayName { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
}
