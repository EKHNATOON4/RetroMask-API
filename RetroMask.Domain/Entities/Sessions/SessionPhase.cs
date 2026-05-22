using RetroMask.Domain.Common;
using RetroMask.Domain.Entities.Discussion;
using RetroMask.Domain.Enums;

namespace RetroMask.Domain.Entities.Sessions;

public class SessionPhase : BaseEntity
{
    public Guid SessionId { get; set; }
    public Session Session { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public SessionPhaseType PhaseType { get; set; }
    public PhaseStatus Status { get; set; } = PhaseStatus.Pending;
    public int Order { get; set; }
    public int? DurationMinutes { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    // Navigation
    public ICollection<DiscussionPoint> DiscussionPoints { get; set; } = new List<DiscussionPoint>();
}
