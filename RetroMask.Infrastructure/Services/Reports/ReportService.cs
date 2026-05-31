using System.Text;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RetroMask.Application.Abstractions;
using RetroMask.Application.Abstractions.Repositories;
using RetroMask.Application.Common;
using RetroMask.Application.Dtos.Reports;
using RetroMask.Application.Services.Reports;
using RetroMask.Domain.Entities.AI;

namespace RetroMask.Infrastructure.Services.Reports;

public class ReportService : IReportService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly ICurrentUser _currentUser;

    public ReportService(IUnitOfWork uow, IMapper mapper, ICurrentUser currentUser)
    {
        _uow = uow;
        _mapper = mapper;
        _currentUser = currentUser;
    }

    public async Task<ApiResponse<ReportDto>> GetSessionReportAsync(Guid sessionId, CancellationToken ct = default)
    {
        var report = await _uow.Repository<AIReport>().Query()
            .Include(r => r.Session)
            .Where(r => r.SessionId == sessionId)
            .OrderByDescending(r => r.GeneratedAt)
            .FirstOrDefaultAsync(ct);

        if (report is null)
            return ApiResponse<ReportDto>.Fail("No report found. Generate one first via the AI endpoint.");

        return ApiResponse<ReportDto>.Ok(_mapper.Map<ReportDto>(report));
    }

    public async Task<ApiResponse<IEnumerable<ReportDto>>> GetTeamReportsAsync(Guid teamId, CancellationToken ct = default)
    {
        var reports = await _uow.Repository<AIReport>().Query()
            .Include(r => r.Session)
            .Where(r => r.Session.TeamId == teamId)
            .OrderByDescending(r => r.GeneratedAt)
            .ToListAsync(ct);

        return ApiResponse<IEnumerable<ReportDto>>.Ok(_mapper.Map<IEnumerable<ReportDto>>(reports));
    }

    public async Task<ApiResponse<byte[]>> ExportReportAsync(Guid sessionId, ExportRequest request, CancellationToken ct = default)
    {
        var report = await _uow.Repository<AIReport>().Query()
            .Include(r => r.Session)
            .Where(r => r.SessionId == sessionId)
            .OrderByDescending(r => r.GeneratedAt)
            .FirstOrDefaultAsync(ct);

        if (report is null)
            return ApiResponse<byte[]>.Fail("No report found.");

        byte[] bytes = request.Format switch
        {
            "html" => Encoding.UTF8.GetBytes(ConvertToHtml(report)),
            "markdown" => Encoding.UTF8.GetBytes(report.MarkdownContent),
            _ => Encoding.UTF8.GetBytes(report.MarkdownContent) // PDF would need a library
        };

        return ApiResponse<byte[]>.Ok(bytes);
    }

    public async Task<ApiResponse> ShareReportAsync(Guid reportId, CancellationToken ct = default)
    {
        var report = await _uow.Repository<AIReport>().GetByIdAsync(reportId, ct);
        if (report is null)
            return ApiResponse.Fail("Report not found.");

        report.IsShared = true;
        _uow.Repository<AIReport>().Update(report);
        await _uow.SaveChangesAsync(ct);

        return ApiResponse.Ok("Report shared.");
    }

    private static string ConvertToHtml(AIReport report)
    {
        var lines = report.MarkdownContent.Split('\n');
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'>");
        sb.AppendLine($"<title>{report.Title}</title>");
        sb.AppendLine("<style>body{font-family:sans-serif;max-width:800px;margin:40px auto;padding:0 20px;line-height:1.6}h1,h2,h3{color:#1e293b}ul{padding-left:20px}</style>");
        sb.AppendLine("</head><body>");

        foreach (var line in lines)
        {
            var trimmed = line.TrimStart();
            if (trimmed.StartsWith("### ")) sb.AppendLine($"<h3>{trimmed[4..]}</h3>");
            else if (trimmed.StartsWith("## ")) sb.AppendLine($"<h2>{trimmed[3..]}</h2>");
            else if (trimmed.StartsWith("# ")) sb.AppendLine($"<h1>{trimmed[2..]}</h1>");
            else if (trimmed.StartsWith("- ")) sb.AppendLine($"<li>{trimmed[2..]}</li>");
            else if (trimmed.StartsWith("**") && trimmed.EndsWith("**")) sb.AppendLine($"<p><strong>{trimmed[2..^2]}</strong></p>");
            else if (!string.IsNullOrWhiteSpace(trimmed)) sb.AppendLine($"<p>{trimmed}</p>");
        }

        sb.AppendLine("</body></html>");
        return sb.ToString();
    }
}
