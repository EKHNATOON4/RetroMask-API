using RetroMask.Domain.Enums;

namespace RetroMask.Application.Dtos.Points;

public class PointDto
{
    public Guid Id { get; set; }
    public Guid PhaseId { get; set; }
    public string AuthorId { get; set; } = string.Empty;
    public string? AuthorName { get; set; }
    public string Content { get; set; } = string.Empty;
    public DiscussionPointType PointType { get; set; }
    public bool IsAnonymous { get; set; }
    public bool IsPinned { get; set; }
    public bool IsHighlighted { get; set; }
    public int Order { get; set; }
    public int VoteCount { get; set; }
    public int CommentCount { get; set; }
    public IEnumerable<TagDto> Tags { get; set; } = new List<TagDto>();
    public IEnumerable<ReactionSummaryDto> Reactions { get; set; } = new List<ReactionSummaryDto>();
    public DateTime CreatedAt { get; set; }
}

public class TagDto
{
    public Guid Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public string? ColorHex { get; set; }
}

public class ReactionSummaryDto
{
    public ReactionType ReactionType { get; set; }
    public int Count { get; set; }
    public bool ReactedByMe { get; set; }
}

public class CreatePointRequest
{
    public string Content { get; set; } = string.Empty;
    public DiscussionPointType PointType { get; set; }
    public bool IsAnonymous { get; set; } = false;
}

public class UpdatePointRequest
{
    public string? Content { get; set; }
    public DiscussionPointType? PointType { get; set; }
}

public class AddTagRequest
{
    public string Label { get; set; } = string.Empty;
    public string? ColorHex { get; set; }
}

public class AddReactionRequest
{
    public ReactionType ReactionType { get; set; }
}

public class CommentDto
{
    public Guid Id { get; set; }
    public Guid DiscussionPointId { get; set; }
    public string AuthorId { get; set; } = string.Empty;
    public string? AuthorName { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsAnonymous { get; set; }
    public Guid? ParentCommentId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AddCommentRequest
{
    public string Content { get; set; } = string.Empty;
    public bool IsAnonymous { get; set; } = false;
    public Guid? ParentCommentId { get; set; }
}
