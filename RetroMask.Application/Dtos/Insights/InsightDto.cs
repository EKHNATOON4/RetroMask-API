namespace RetroMask.Application.Dtos.Insights;

public class UserInsightDto
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public int TotalSessionsAttended { get; set; }
    public int TotalPointsSubmitted { get; set; }
    public int TotalVotesCast { get; set; }
    public int TotalActionItemsAssigned { get; set; }
    public int TotalActionItemsCompleted { get; set; }
    public double ActionItemCompletionRate { get; set; }
    public double AverageEngagementScore { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
}

public class GrowthSnapshotDto
{
    public int Month { get; set; }
    public int Year { get; set; }
    public string MonthLabel { get; set; } = string.Empty;
    public int SessionsAttended { get; set; }
    public int PointsSubmitted { get; set; }
    public int ActionItemsCompleted { get; set; }
    public double EngagementScore { get; set; }
}
