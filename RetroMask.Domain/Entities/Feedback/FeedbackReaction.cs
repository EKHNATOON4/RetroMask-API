using RetroMask.Domain.Common;
using RetroMask.Domain.Entities.Identity;
using RetroMask.Domain.Enums;

namespace RetroMask.Domain.Entities.Feedback;

public class FeedbackReaction : BaseEntity
{
    public Guid FriendFeedbackId { get; set; }
    public FriendFeedback FriendFeedback { get; set; } = null!;

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public FeedbackReactionType ReactionType { get; set; }
}
