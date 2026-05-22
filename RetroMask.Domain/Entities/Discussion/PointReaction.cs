using RetroMask.Domain.Common;
using RetroMask.Domain.Entities.Identity;
using RetroMask.Domain.Enums;

namespace RetroMask.Domain.Entities.Discussion;

public class PointReaction : BaseEntity
{
    public Guid DiscussionPointId { get; set; }
    public DiscussionPoint DiscussionPoint { get; set; } = null!;

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public ReactionType ReactionType { get; set; }
}
