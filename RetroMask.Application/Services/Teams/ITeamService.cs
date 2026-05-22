using RetroMask.Application.Common;
using RetroMask.Application.Dtos.Teams;

namespace RetroMask.Application.Services.Teams;

public interface ITeamService
{
    Task<ApiResponse<TeamDto>> CreateTeamAsync(CreateTeamRequest request, CancellationToken ct = default);
    Task<ApiResponse<TeamDto>> GetTeamByIdAsync(Guid teamId, CancellationToken ct = default);
    Task<ApiResponse<IEnumerable<TeamDto>>> GetMyTeamsAsync(CancellationToken ct = default);
    Task<ApiResponse<TeamDto>> UpdateTeamAsync(Guid teamId, UpdateTeamRequest request, CancellationToken ct = default);
    Task<ApiResponse> DeleteTeamAsync(Guid teamId, CancellationToken ct = default);
    Task<ApiResponse> InviteMemberAsync(Guid teamId, InviteMemberRequest request, CancellationToken ct = default);
    Task<ApiResponse> AcceptInvitationAsync(string token, CancellationToken ct = default);
    Task<ApiResponse> DeclineInvitationAsync(string token, CancellationToken ct = default);
    Task<ApiResponse> RemoveMemberAsync(Guid teamId, string userId, CancellationToken ct = default);
    Task<ApiResponse<IEnumerable<TeamMemberDto>>> GetMembersAsync(Guid teamId, CancellationToken ct = default);
    Task<ApiResponse> UpdateMemberRoleAsync(Guid teamId, string userId, UpdateMemberRoleRequest request, CancellationToken ct = default);
}
