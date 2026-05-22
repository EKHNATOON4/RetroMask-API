using RetroMask.Domain.Common;
using RetroMask.Domain.Entities.Teams;
using RetroMask.Domain.Entities.Identity;
using RetroMask.Domain.Entities.AI;
using RetroMask.Domain.Enums;

namespace RetroMask.Domain.Entities.Sessions;

public class Session : BaseEntity, ISoftDelete
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public SessionStatus Status { get; set; } = SessionStatus.Draft;

    public Guid TeamId { get; set; }
    public Team Team { get; set; } = null!;

    public string FacilitatorId { get; set; } = string.Empty;
    public ApplicationUser Facilitator { get; set; } = null!;

    public Guid? TemplateId { get; set; }
    public SessionTemplate? Template { get; set; }

    public bool IsAnonymous { get; set; } = false;
    public bool VotingEnabled { get; set; } = true;
    public int MaxVotesPerUser { get; set; } = 3;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? ScheduledAt { get; set; }

    // ISoftDelete
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    // Navigation
    public ICollection<SessionMember> Members { get; set; } = new List<SessionMember>();
    public ICollection<SessionPhase> Phases { get; set; } = new List<SessionPhase>();
    public ICollection<ModerationLog> ModerationLogs { get; set; } = new List<ModerationLog>();
    public ICollection<AIReport> AIReports { get; set; } = new List<AIReport>();
}
