using RetroMask.Domain.Common;

namespace RetroMask.Domain.Entities.AI;

public class AICluster : BaseEntity
{
    public Guid SessionId { get; set; }
    public string Label { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string? ColorHex { get; set; }
    public int PointCount { get; set; } = 0;

    /// <summary>JSON array of DiscussionPoint IDs belonging to this cluster.</summary>
    public string PointIdsJson { get; set; } = "[]";
}
