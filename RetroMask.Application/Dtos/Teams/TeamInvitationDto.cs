using RetroMask.Domain.Enums;

namespace RetroMask.Application.Dtos.Teams;

public class TeamInvitationDto
{
    public Guid Id { get; set; }
    public Guid TeamId { get; set; }
    public string TeamName { get; set; } = string.Empty;
    public string InvitedEmail { get; set; } = string.Empty;
    public string InvitedByName { get; set; } = string.Empty;
    public TeamMemberRole AssignedRole { get; set; }
    public InvitationStatus Status { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
