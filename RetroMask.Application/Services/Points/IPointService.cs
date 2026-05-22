using RetroMask.Application.Common;
using RetroMask.Application.Dtos.Points;

namespace RetroMask.Application.Services.Points;

public interface IPointService
{
    Task<ApiResponse<PointDto>> CreatePointAsync(Guid phaseId, CreatePointRequest request, CancellationToken ct = default);
    Task<ApiResponse<PointDto>> GetPointByIdAsync(Guid pointId, CancellationToken ct = default);
    Task<ApiResponse<IEnumerable<PointDto>>> GetPhasePointsAsync(Guid phaseId, CancellationToken ct = default);
    Task<ApiResponse<PointDto>> UpdatePointAsync(Guid pointId, UpdatePointRequest request, CancellationToken ct = default);
    Task<ApiResponse> DeletePointAsync(Guid pointId, CancellationToken ct = default);
    Task<ApiResponse<PointDto>> PinPointAsync(Guid pointId, bool pin, CancellationToken ct = default);
    Task<ApiResponse> AddTagAsync(Guid pointId, AddTagRequest request, CancellationToken ct = default);
    Task<ApiResponse> RemoveTagAsync(Guid pointId, Guid tagId, CancellationToken ct = default);
    Task<ApiResponse> AddReactionAsync(Guid pointId, AddReactionRequest request, CancellationToken ct = default);
    Task<ApiResponse<CommentDto>> AddCommentAsync(Guid pointId, AddCommentRequest request, CancellationToken ct = default);
}
