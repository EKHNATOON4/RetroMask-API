using RetroMask.Domain.Common;
using RetroMask.Domain.Entities.Identity;

namespace RetroMask.Domain.Entities.Sessions;

public class SessionMember : BaseEntity
{
    public Guid SessionId { get; set; }
    public Session Session { get; set; } = null!;

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public string? MaskName { get; set; }
    public string? AvatarUrl { get; set; }
    public bool IsOnline { get; set; } = false;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LeftAt { get; set; }
}
