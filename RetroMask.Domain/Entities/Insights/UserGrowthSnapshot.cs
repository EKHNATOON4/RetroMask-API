using RetroMask.Domain.Common;
using RetroMask.Domain.Entities.Identity;

namespace RetroMask.Domain.Entities.Insights;

public class UserGrowthSnapshot : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public int Month { get; set; }
    public int Year { get; set; }
    public int SessionsAttended { get; set; } = 0;
    public int PointsSubmitted { get; set; } = 0;
    public int ActionItemsCompleted { get; set; } = 0;
    public double EngagementScore { get; set; } = 0;
    public string? NoteJson { get; set; }
    public DateTime SnapshotDate { get; set; } = DateTime.UtcNow;
}
