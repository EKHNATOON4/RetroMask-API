using RetroMask.Domain.Common;
using RetroMask.Domain.Entities.Identity;

namespace RetroMask.Domain.Entities.Discussion;

public class DiscussionComment : BaseEntity, ISoftDelete
{
    public Guid DiscussionPointId { get; set; }
    public DiscussionPoint DiscussionPoint { get; set; } = null!;

    public string AuthorId { get; set; } = string.Empty;
    public ApplicationUser Author { get; set; } = null!;

    public string Content { get; set; } = string.Empty;
    public bool IsAnonymous { get; set; } = false;

    public Guid? ParentCommentId { get; set; }
    public DiscussionComment? ParentComment { get; set; }
    public ICollection<DiscussionComment> Replies { get; set; } = new List<DiscussionComment>();

    // ISoftDelete
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}
