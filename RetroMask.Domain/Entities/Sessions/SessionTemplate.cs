using RetroMask.Domain.Common;

namespace RetroMask.Domain.Entities.Sessions;

public class SessionTemplate : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsDefault { get; set; } = false;
    public bool IsPublic { get; set; } = true;
    public string? CreatedByUserId { get; set; }

    /// <summary>JSON serialized list of phase configurations.</summary>
    public string PhasesJson { get; set; } = "[]";

    // Navigation
    public ICollection<Session> Sessions { get; set; } = new List<Session>();
}
