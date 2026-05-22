using RetroMask.Domain.Common;
using RetroMask.Domain.Enums;

namespace RetroMask.Domain.Entities.AI;

public class AIInsight : BaseEntity
{
    public Guid SessionId { get; set; }
    public AIInsightType InsightType { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? ModelUsed { get; set; }
    public string? PromptUsed { get; set; }
    public int? TokensUsed { get; set; }
    public double? ConfidenceScore { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
