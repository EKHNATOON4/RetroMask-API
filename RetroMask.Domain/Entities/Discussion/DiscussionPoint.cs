using RetroMask.Domain.Common;
using RetroMask.Domain.Entities.Identity;
using RetroMask.Domain.Entities.Sessions;
using RetroMask.Domain.Entities.Voting;
using RetroMask.Domain.Enums;

namespace RetroMask.Domain.Entities.Discussion;

public class DiscussionPoint : BaseEntity, ISoftDelete
{
    public Guid PhaseId { get; set; }
    public SessionPhase Phase { get; set; } = null!;

    public string AuthorId { get; set; } = string.Empty;
    public ApplicationUser Author { get; set; } = null!;

    public string Content { get; set; } = string.Empty;
    public DiscussionPointType PointType { get; set; }
    public bool IsAnonymous { get; set; } = false;
    public bool IsPinned { get; set; } = false;
    public bool IsHighlighted { get; set; } = false;
    public int Order { get; set; } = 0;

    // ISoftDelete
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    // Navigation
    public ICollection<PointTag> Tags { get; set; } = new List<PointTag>();
    public ICollection<PointReaction> Reactions { get; set; } = new List<PointReaction>();
    public ICollection<DiscussionComment> Comments { get; set; } = new List<DiscussionComment>();
    public ICollection<Vote> Votes { get; set; } = new List<Vote>();
    public VoteSummary? VoteSummary { get; set; }
}
