using RetroMask.Domain.Common;

namespace RetroMask.Domain.Entities.Discussion;

public class PointTag : BaseEntity
{
    public Guid DiscussionPointId { get; set; }
    public DiscussionPoint DiscussionPoint { get; set; } = null!;

    public string Label { get; set; } = string.Empty;
    public string? ColorHex { get; set; }
}
