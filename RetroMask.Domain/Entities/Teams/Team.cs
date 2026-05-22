using RetroMask.Domain.Common;
using RetroMask.Domain.Entities.Sessions;

namespace RetroMask.Domain.Entities.Teams;

public class Team : BaseEntity, ISoftDelete
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? AvatarUrl { get; set; }
    public string? InviteCode { get; set; }
    public bool IsPublic { get; set; } = false;

    // ISoftDelete
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    // Navigation
    public ICollection<TeamMember> Members { get; set; } = new List<TeamMember>();
    public ICollection<TeamInvitation> Invitations { get; set; } = new List<TeamInvitation>();
    public ICollection<Session> Sessions { get; set; } = new List<Session>();
}
