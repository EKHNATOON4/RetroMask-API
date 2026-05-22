using RetroMask.Domain.Common;
using RetroMask.Domain.Entities.Identity;
using RetroMask.Domain.Entities.Sessions;
using RetroMask.Domain.Enums;

namespace RetroMask.Domain.Entities.ActionItems;

public class ActionItem : BaseEntity, ISoftDelete
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    public Guid SessionId { get; set; }
    public Session Session { get; set; } = null!;

    public string AssignedToId { get; set; } = string.Empty;
    public ApplicationUser AssignedTo { get; set; } = null!;

    public string CreatedById { get; set; } = string.Empty;
    public new ApplicationUser CreatedBy { get; set; } = null!;

    public ActionItemStatus Status { get; set; } = ActionItemStatus.Open;
    public ActionItemPriority Priority { get; set; } = ActionItemPriority.Medium;
    public DateTime? DueDate { get; set; }
    public DateTime? CompletedAt { get; set; }

    // ISoftDelete
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    // Navigation
    public ICollection<ActionItemUpdate> Updates { get; set; } = new List<ActionItemUpdate>();
}
