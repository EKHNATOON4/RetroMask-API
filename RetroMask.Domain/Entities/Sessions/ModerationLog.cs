using RetroMask.Domain.Common;
using RetroMask.Domain.Entities.Identity;

namespace RetroMask.Domain.Entities.Sessions;

public class ModerationLog : BaseEntity
{
    public Guid SessionId { get; set; }
    public Session Session { get; set; } = null!;

    public string ModeratorId { get; set; } = string.Empty;
    public ApplicationUser Moderator { get; set; } = null!;

    public string Action { get; set; } = string.Empty;
    public string? TargetEntityType { get; set; }
    public Guid? TargetEntityId { get; set; }
    public string? Reason { get; set; }
    public DateTime LoggedAt { get; set; } = DateTime.UtcNow;
}
