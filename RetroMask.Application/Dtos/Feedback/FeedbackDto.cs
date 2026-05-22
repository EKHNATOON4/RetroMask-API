using RetroMask.Domain.Enums;

namespace RetroMask.Application.Dtos.Feedback;

public class FeedbackDto
{
    public Guid Id { get; set; }
    public string GiverId { get; set; } = string.Empty;
    public string? GiverName { get; set; }
    public string ReceiverId { get; set; } = string.Empty;
    public string ReceiverName { get; set; } = string.Empty;
    public Guid? SessionId { get; set; }
    public string Content { get; set; } = string.Empty;
    public FeedbackType FeedbackType { get; set; }
    public bool IsAnonymous { get; set; }
    public int? Rating { get; set; }
    public IEnumerable<FeedbackReactionSummaryDto> Reactions { get; set; } = new List<FeedbackReactionSummaryDto>();
    public DateTime CreatedAt { get; set; }
}

public class FeedbackReactionSummaryDto
{
    public FeedbackReactionType ReactionType { get; set; }
    public int Count { get; set; }
}

public class CreateFeedbackRequest
{
    public string ReceiverId { get; set; } = string.Empty;
    public Guid? SessionId { get; set; }
    public string Content { get; set; } = string.Empty;
    public FeedbackType FeedbackType { get; set; } = FeedbackType.General;
    public bool IsAnonymous { get; set; } = false;
    public int? Rating { get; set; }
}

public class AddFeedbackReactionRequest
{
    public FeedbackReactionType ReactionType { get; set; }
}
