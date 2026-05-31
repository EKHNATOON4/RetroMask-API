using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetroMask.Application.Common;
using RetroMask.Application.Dtos.Teams;
using RetroMask.Application.Services.Teams;

namespace RetroMask.API.Controllers.Teams;

/// <summary>
/// Team management: create, update, delete teams and manage memberships.
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class TeamsController : ControllerBase
{
    private readonly ITeamService _teamService;

    public TeamsController(ITeamService teamService)
    {
        _teamService = teamService;
    }

    /// <summary>List all teams the current user belongs to.</summary>
    /// <response code="200">Returns the user's teams.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyTeams(CancellationToken ct)
        => Ok(await _teamService.GetMyTeamsAsync(ct));

    /// <summary>Get a team by its ID.</summary>
    /// <param name="id">Team ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Returns the team details.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<TeamDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => Ok(await _teamService.GetTeamByIdAsync(id, ct));

    /// <summary>Create a new team. The creator is automatically added as Owner.</summary>
    /// <param name="request">Team name and optional description.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="201">Team created successfully.</response>
    /// <response code="400">Validation failed.</response>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<TeamDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateTeamRequest request, CancellationToken ct)
    {
        var result = await _teamService.CreateTeamAsync(request, ct);
        if (!result.Success || result.Data is null) return BadRequest(result);
        return CreatedAtAction(nameof(GetById), new { id = result.Data.Id }, result);
    }

    /// <summary>Update team name, description, or visibility.</summary>
    /// <param name="id">Team ID.</param>
    /// <param name="request">Fields to update.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Team updated.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<TeamDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTeamRequest request, CancellationToken ct)
        => Ok(await _teamService.UpdateTeamAsync(id, request, ct));

    /// <summary>Soft-delete a team (Owner only).</summary>
    /// <param name="id">Team ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Team deleted.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        => Ok(await _teamService.DeleteTeamAsync(id, ct));

    /// <summary>List all members of a team.</summary>
    /// <param name="id">Team ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Returns the member list.</response>
    [HttpGet("{id:guid}/members")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMembers(Guid id, CancellationToken ct)
        => Ok(await _teamService.GetMembersAsync(id, ct));

    /// <summary>Invite a user to the team by email.</summary>
    /// <param name="id">Team ID.</param>
    /// <param name="request">Invitee email and optional role.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Invitation sent.</response>
    [HttpPost("{id:guid}/invite")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Invite(Guid id, [FromBody] InviteMemberRequest request, CancellationToken ct)
        => Ok(await _teamService.InviteMemberAsync(id, request, ct));

    /// <summary>Accept a team invitation using the invite token.</summary>
    /// <param name="token">Invitation token from the invite email.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Invitation accepted.</response>
    [HttpPost("invitations/{token}/accept")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> AcceptInvitation(string token, CancellationToken ct)
        => Ok(await _teamService.AcceptInvitationAsync(token, ct));

    /// <summary>Decline a team invitation.</summary>
    /// <param name="token">Invitation token.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Invitation declined.</response>
    [HttpPost("invitations/{token}/decline")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeclineInvitation(string token, CancellationToken ct)
        => Ok(await _teamService.DeclineInvitationAsync(token, ct));

    /// <summary>Remove a member from the team (Admin/Owner only).</summary>
    /// <param name="id">Team ID.</param>
    /// <param name="userId">User ID of the member to remove.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Member removed.</response>
    [HttpDelete("{id:guid}/members/{userId}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> RemoveMember(Guid id, string userId, CancellationToken ct)
        => Ok(await _teamService.RemoveMemberAsync(id, userId, ct));

    /// <summary>Change a team member's role (Admin/Owner only).</summary>
    /// <param name="id">Team ID.</param>
    /// <param name="userId">User ID of the member.</param>
    /// <param name="request">New role assignment.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Role updated.</response>
    [HttpPut("{id:guid}/members/{userId}/role")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateMemberRole(Guid id, string userId, [FromBody] UpdateMemberRoleRequest request, CancellationToken ct)
        => Ok(await _teamService.UpdateMemberRoleAsync(id, userId, request, ct));
}
