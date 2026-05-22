using RetroMask.Domain.Enums;

namespace RetroMask.Application.Dtos.Teams;

public class TeamMemberDto
{
    public string UserId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public TeamMemberRole Role { get; set; }
    public DateTime JoinedAt { get; set; }
    public bool IsActive { get; set; }
}

public class InviteMemberRequest
{
    public string Email { get; set; } = string.Empty;
    public TeamMemberRole Role { get; set; } = TeamMemberRole.Member;
}

public class UpdateMemberRoleRequest
{
    public TeamMemberRole NewRole { get; set; }
}
