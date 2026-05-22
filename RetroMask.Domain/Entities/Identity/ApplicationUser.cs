using Microsoft.AspNetCore.Identity;
using RetroMask.Domain.Entities.Teams;
using RetroMask.Domain.Entities.Notifications;
using RetroMask.Domain.Entities.Insights;
using RetroMask.Domain.Enums;

namespace RetroMask.Domain.Entities.Identity;

public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    public UserRole Role { get; set; } = UserRole.User;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }

    // Navigation
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<TeamMember> TeamMemberships { get; set; } = new List<TeamMember>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<UserInsight> Insights { get; set; } = new List<UserInsight>();
}
