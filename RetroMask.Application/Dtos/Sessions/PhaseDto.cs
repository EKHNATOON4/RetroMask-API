using RetroMask.Domain.Enums;

namespace RetroMask.Application.Dtos.Sessions;

public class PhaseDto
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public SessionPhaseType PhaseType { get; set; }
    public PhaseStatus Status { get; set; }
    public int Order { get; set; }
    public int? DurationMinutes { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int PointCount { get; set; }
}

public class ReorderPhasesRequest
{
    public IEnumerable<PhaseOrderItem> Order { get; set; } = new List<PhaseOrderItem>();
}

public class PhaseOrderItem
{
    public Guid PhaseId { get; set; }
    public int Order { get; set; }
}

public class ExtendPhaseRequest
{
    public int AdditionalMinutes { get; set; } = 5;
}
