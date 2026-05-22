using RetroMask.Domain.Enums;

namespace RetroMask.Application.Dtos.Sessions;

public class SessionDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public SessionStatus Status { get; set; }
    public Guid TeamId { get; set; }
    public string TeamName { get; set; } = string.Empty;
    public string FacilitatorId { get; set; } = string.Empty;
    public string FacilitatorName { get; set; } = string.Empty;
    public bool IsAnonymous { get; set; }
    public bool VotingEnabled { get; set; }
    public int MaxVotesPerUser { get; set; }
    public int MemberCount { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public IEnumerable<PhaseDto> Phases { get; set; } = new List<PhaseDto>();
}

public class SessionSummaryDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public SessionStatus Status { get; set; }
    public int MemberCount { get; set; }
    public int PointCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class CreateSessionRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid TeamId { get; set; }
    public Guid? TemplateId { get; set; }
    public bool IsAnonymous { get; set; } = false;
    public bool VotingEnabled { get; set; } = true;
    public int MaxVotesPerUser { get; set; } = 3;
    public DateTime? ScheduledAt { get; set; }
}

public class UpdateSessionRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public bool? IsAnonymous { get; set; }
    public bool? VotingEnabled { get; set; }
    public int? MaxVotesPerUser { get; set; }
}

public class JoinSessionRequest
{
    public string? MaskName { get; set; }
}
