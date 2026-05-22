using RetroMask.Application.Common;
using RetroMask.Application.Dtos.Feedback;

namespace RetroMask.Application.Services.Feedback;

public interface IFeedbackService
{
    Task<ApiResponse<FeedbackDto>> CreateFeedbackAsync(CreateFeedbackRequest request, CancellationToken ct = default);
    Task<ApiResponse<FeedbackDto>> GetFeedbackByIdAsync(Guid feedbackId, CancellationToken ct = default);
    Task<ApiResponse<PagedResult<FeedbackDto>>> GetReceivedFeedbackAsync(int page, int pageSize, CancellationToken ct = default);
    Task<ApiResponse<PagedResult<FeedbackDto>>> GetGivenFeedbackAsync(int page, int pageSize, CancellationToken ct = default);
    Task<ApiResponse> DeleteFeedbackAsync(Guid feedbackId, CancellationToken ct = default);
    Task<ApiResponse> AddReactionAsync(Guid feedbackId, AddFeedbackReactionRequest request, CancellationToken ct = default);
}
