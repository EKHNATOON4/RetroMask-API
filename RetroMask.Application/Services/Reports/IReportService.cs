using RetroMask.Application.Common;
using RetroMask.Application.Dtos.Reports;

namespace RetroMask.Application.Services.Reports;

public interface IReportService
{
    Task<ApiResponse<ReportDto>> GetSessionReportAsync(Guid sessionId, CancellationToken ct = default);
    Task<ApiResponse<byte[]>> ExportReportAsync(Guid sessionId, ExportRequest request, CancellationToken ct = default);
    Task<ApiResponse> ShareReportAsync(Guid reportId, CancellationToken ct = default);
    Task<ApiResponse<IEnumerable<ReportDto>>> GetTeamReportsAsync(Guid teamId, CancellationToken ct = default);
}
