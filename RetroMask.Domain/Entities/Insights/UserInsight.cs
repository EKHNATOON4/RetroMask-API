using RetroMask.Domain.Common;
using RetroMask.Domain.Entities.Identity;

namespace RetroMask.Domain.Entities.Insights;

public class UserInsight : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public int TotalSessionsAttended { get; set; } = 0;
    public int TotalPointsSubmitted { get; set; } = 0;
    public int TotalVotesCast { get; set; } = 0;
    public int TotalActionItemsAssigned { get; set; } = 0;
    public int TotalActionItemsCompleted { get; set; } = 0;
    public double AverageEngagementScore { get; set; } = 0;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public string? InsightJson { get; set; }
}
