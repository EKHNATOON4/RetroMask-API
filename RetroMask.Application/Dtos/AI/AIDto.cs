using RetroMask.Domain.Enums;

namespace RetroMask.Application.Dtos.AI;

public class AIInsightDto
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public AIInsightType InsightType { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? ModelUsed { get; set; }
    public double? ConfidenceScore { get; set; }
    public DateTime GeneratedAt { get; set; }
}

public class AIClusterDto
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public string Label { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string? ColorHex { get; set; }
    public int PointCount { get; set; }
    public IEnumerable<Guid> PointIds { get; set; } = new List<Guid>();
}

public class AIReportDto
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string MarkdownContent { get; set; } = string.Empty;
    public string? HtmlContent { get; set; }
    public string? ModelUsed { get; set; }
    public bool IsShared { get; set; }
    public DateTime GeneratedAt { get; set; }
}
