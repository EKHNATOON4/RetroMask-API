using RetroMask.Application.Common;
using RetroMask.Application.Dtos.Sessions;

namespace RetroMask.Application.Services.Phases;

public interface IPhaseService
{
    Task<ApiResponse<PhaseDto>> GetPhaseByIdAsync(Guid phaseId, CancellationToken ct = default);
    Task<ApiResponse<IEnumerable<PhaseDto>>> GetSessionPhasesAsync(Guid sessionId, CancellationToken ct = default);
    Task<ApiResponse<PhaseDto>> ActivatePhaseAsync(Guid phaseId, CancellationToken ct = default);
    Task<ApiResponse<PhaseDto>> CompletePhaseAsync(Guid phaseId, CancellationToken ct = default);
    Task<ApiResponse<PhaseDto>> SkipPhaseAsync(Guid phaseId, CancellationToken ct = default);
    Task<ApiResponse<PhaseDto>> ReorderPhasesAsync(Guid sessionId, ReorderPhasesRequest request, CancellationToken ct = default);
}
