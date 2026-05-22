using RetroMask.Domain.Common;
using RetroMask.Domain.Entities.Identity;
using RetroMask.Domain.Enums;

namespace RetroMask.Domain.Entities.ActionItems;

public class ActionItemUpdate : BaseEntity
{
    public Guid ActionItemId { get; set; }
    public ActionItem ActionItem { get; set; } = null!;

    public string AuthorId { get; set; } = string.Empty;
    public ApplicationUser Author { get; set; } = null!;

    public string Note { get; set; } = string.Empty;
    public ActionItemStatus? StatusChange { get; set; }
    public int? ProgressPercent { get; set; }
}
