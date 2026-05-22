using RetroMask.Domain.Common;
using RetroMask.Domain.Entities.Identity;
using RetroMask.Domain.Enums;

namespace RetroMask.Domain.Entities.Feedback;

public class FriendFeedback : BaseEntity, ISoftDelete
{
    public string GiverId { get; set; } = string.Empty;
    public ApplicationUser Giver { get; set; } = null!;

    public string ReceiverId { get; set; } = string.Empty;
    public ApplicationUser Receiver { get; set; } = null!;

    public Guid? SessionId { get; set; }

    public string Content { get; set; } = string.Empty;
    public FeedbackType FeedbackType { get; set; } = FeedbackType.General;
    public bool IsAnonymous { get; set; } = false;
    public int? Rating { get; set; }

    // ISoftDelete
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    // Navigation
    public ICollection<FeedbackReaction> Reactions { get; set; } = new List<FeedbackReaction>();
}
