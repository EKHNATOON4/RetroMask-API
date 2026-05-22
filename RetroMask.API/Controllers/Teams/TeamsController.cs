using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetroMask.Application.Dtos.Teams;
using RetroMask.Application.Services.Teams;

namespace RetroMask.API.Controllers.Teams;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class TeamsController : ControllerBase
{
    private readonly ITeamService _teamService;

    public TeamsController(ITeamService teamService)
    {
        _teamService = teamService;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyTeams(CancellationToken ct)
        => Ok(await _teamService.GetMyTeamsAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => Ok(await _teamService.GetTeamByIdAsync(id, ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTeamRequest request, CancellationToken ct)
    {
        var result = await _teamService.CreateTeamAsync(request, ct);
        return result.Success ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result) : BadRequest(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTeamRequest request, CancellationToken ct)
        => Ok(await _teamService.UpdateTeamAsync(id, request, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        => Ok(await _teamService.DeleteTeamAsync(id, ct));

    [HttpGet("{id:guid}/members")]
    public async Task<IActionResult> GetMembers(Guid id, CancellationToken ct)
        => Ok(await _teamService.GetMembersAsync(id, ct));

    [HttpPost("{id:guid}/invite")]
    public async Task<IActionResult> Invite(Guid id, [FromBody] InviteMemberRequest request, CancellationToken ct)
        => Ok(await _teamService.InviteMemberAsync(id, request, ct));

    [HttpPost("invitations/{token}/accept")]
    public async Task<IActionResult> AcceptInvitation(string token, CancellationToken ct)
        => Ok(await _teamService.AcceptInvitationAsync(token, ct));

    [HttpPost("invitations/{token}/decline")]
    public async Task<IActionResult> DeclineInvitation(string token, CancellationToken ct)
        => Ok(await _teamService.DeclineInvitationAsync(token, ct));

    [HttpDelete("{id:guid}/members/{userId}")]
    public async Task<IActionResult> RemoveMember(Guid id, string userId, CancellationToken ct)
        => Ok(await _teamService.RemoveMemberAsync(id, userId, ct));

    [HttpPut("{id:guid}/members/{userId}/role")]
    public async Task<IActionResult> UpdateMemberRole(Guid id, string userId, [FromBody] UpdateMemberRoleRequest request, CancellationToken ct)
        => Ok(await _teamService.UpdateMemberRoleAsync(id, userId, request, ct));
}
