using RetroMask.Domain.Common;
using RetroMask.Domain.Entities.Discussion;
using RetroMask.Domain.Entities.Identity;
using RetroMask.Domain.Enums;

namespace RetroMask.Domain.Entities.Voting;

public class Vote : BaseEntity
{
    public Guid DiscussionPointId { get; set; }
    public DiscussionPoint DiscussionPoint { get; set; } = null!;

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public VoteType VoteType { get; set; } = VoteType.Up;
}
