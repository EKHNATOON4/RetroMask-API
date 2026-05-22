using RetroMask.Domain.Common;
using RetroMask.Domain.Entities.Identity;
using RetroMask.Domain.Enums;

namespace RetroMask.Domain.Entities.Teams;

public class TeamInvitation : BaseEntity
{
    public Guid TeamId { get; set; }
    public Team Team { get; set; } = null!;

    public string InvitedEmail { get; set; } = string.Empty;
    public string? InvitedUserId { get; set; }
    public ApplicationUser? InvitedUser { get; set; }

    public string InvitedById { get; set; } = string.Empty;
    public ApplicationUser InvitedBy { get; set; } = null!;

    public string Token { get; set; } = string.Empty;
    public InvitationStatus Status { get; set; } = InvitationStatus.Pending;
    public TeamMemberRole AssignedRole { get; set; } = TeamMemberRole.Member;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RespondedAt { get; set; }
}
