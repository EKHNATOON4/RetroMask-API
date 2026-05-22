using RetroMask.Domain.Common;
using RetroMask.Domain.Entities.Identity;
using RetroMask.Domain.Enums;

namespace RetroMask.Domain.Entities.Teams;

public class TeamMember : BaseEntity
{
    public Guid TeamId { get; set; }
    public Team Team { get; set; } = null!;

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public TeamMemberRole Role { get; set; } = TeamMemberRole.Member;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}
