using RetroMask.Domain.Common;
using RetroMask.Domain.Entities.Sessions;

namespace RetroMask.Domain.Entities.AI;

public class AIReport : BaseEntity
{
    public Guid SessionId { get; set; }
    public Session Session { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string MarkdownContent { get; set; } = string.Empty;
    public string? HtmlContent { get; set; }
    public string? ModelUsed { get; set; }
    public int? TotalTokensUsed { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public bool IsShared { get; set; } = false;
}
