using RetroMask.Application.Common;
using RetroMask.Application.Dtos.ActionItems;

namespace RetroMask.Application.Services.ActionItems;

public interface IActionItemService
{
    Task<ApiResponse<ActionItemDto>> CreateAsync(CreateActionItemRequest request, CancellationToken ct = default);
    Task<ApiResponse<ActionItemDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ApiResponse<PagedResult<ActionItemDto>>> GetSessionActionItemsAsync(Guid sessionId, int page, int pageSize, CancellationToken ct = default);
    Task<ApiResponse<PagedResult<ActionItemDto>>> GetMyActionItemsAsync(int page, int pageSize, CancellationToken ct = default);
    Task<ApiResponse<ActionItemDto>> UpdateAsync(Guid id, UpdateActionItemRequest request, CancellationToken ct = default);
    Task<ApiResponse> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<ApiResponse<ActionItemDto>> AddUpdateAsync(Guid id, AddActionItemUpdateRequest request, CancellationToken ct = default);
}
