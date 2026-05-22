using RetroMask.Domain.Common;
using RetroMask.Domain.Enums;

namespace RetroMask.Domain.Entities.AI;

public class SentimentScore : BaseEntity
{
    public Guid SessionId { get; set; }
    public Guid? DiscussionPointId { get; set; }

    public SentimentType Sentiment { get; set; }
    public double Score { get; set; }
    public double PositiveScore { get; set; }
    public double NeutralScore { get; set; }
    public double NegativeScore { get; set; }
    public string? ModelUsed { get; set; }
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
}
