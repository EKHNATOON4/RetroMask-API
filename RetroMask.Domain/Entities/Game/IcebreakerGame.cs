using RetroMask.Domain.Common;
using RetroMask.Domain.Entities.Sessions;

namespace RetroMask.Domain.Entities.Game;

public class IcebreakerGame : BaseEntity
{
    public Guid SessionId { get; set; }
    public Session Session { get; set; } = null!;

    public string GameType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ConfigJson { get; set; }
    public bool IsCompleted { get; set; } = false;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    // Navigation
    public ICollection<GameResult> Results { get; set; } = new List<GameResult>();
}
