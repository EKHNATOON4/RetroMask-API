namespace RetroMask.Application.Dtos.Reports;

public class ReportDto
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public string SessionTitle { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string MarkdownContent { get; set; } = string.Empty;
    public string? HtmlContent { get; set; }
    public bool IsShared { get; set; }
    public DateTime GeneratedAt { get; set; }
}

public class ExportRequest
{
    public string Format { get; set; } = "pdf"; // pdf | markdown | html
}
