using RetroMask.Domain.Common;
using RetroMask.Domain.Entities.Identity;

namespace RetroMask.Domain.Entities.Game;

public class GameResult : BaseEntity
{
    public Guid IcebreakerGameId { get; set; }
    public IcebreakerGame IcebreakerGame { get; set; } = null!;

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public string? Answer { get; set; }
    public int? Score { get; set; }
    public bool IsCorrect { get; set; } = false;
    public string? MetadataJson { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
}
