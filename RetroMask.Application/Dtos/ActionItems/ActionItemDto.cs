using RetroMask.Domain.Enums;

namespace RetroMask.Application.Dtos.ActionItems;

public class ActionItemDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid SessionId { get; set; }
    public string AssignedToId { get; set; } = string.Empty;
    public string AssignedToName { get; set; } = string.Empty;
    public ActionItemStatus Status { get; set; }
    public ActionItemPriority Priority { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int ProgressPercent { get; set; }
    public IEnumerable<ActionItemUpdateDto> Updates { get; set; } = new List<ActionItemUpdateDto>();
    public DateTime CreatedAt { get; set; }
}

public class ActionItemUpdateDto
{
    public Guid Id { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public ActionItemStatus? StatusChange { get; set; }
    public int? ProgressPercent { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateActionItemRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid SessionId { get; set; }
    public string AssignedToId { get; set; } = string.Empty;
    public ActionItemPriority Priority { get; set; } = ActionItemPriority.Medium;
    public DateTime? DueDate { get; set; }
}

public class UpdateActionItemRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? AssignedToId { get; set; }
    public ActionItemStatus? Status { get; set; }
    public ActionItemPriority? Priority { get; set; }
    public DateTime? DueDate { get; set; }
}

public class AddActionItemUpdateRequest
{
    public string Note { get; set; } = string.Empty;
    public ActionItemStatus? StatusChange { get; set; }
    public int? ProgressPercent { get; set; }
}
