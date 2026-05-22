using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetroMask.Application.Dtos.Voting;
using RetroMask.Application.Services.Voting;

namespace RetroMask.API.Controllers.Sessions;

[Authorize]
[ApiController]
[Route("api/points/{pointId:guid}/votes")]
public class VotingController : ControllerBase
{
    private readonly IVotingService _votingService;

    public VotingController(IVotingService votingService)
    {
        _votingService = votingService;
    }

    [HttpGet]
    public async Task<IActionResult> GetSummary(Guid pointId, CancellationToken ct)
        => Ok(await _votingService.GetVoteSummaryAsync(pointId, ct));

    [HttpPost]
    public async Task<IActionResult> Cast(Guid pointId, [FromBody] CastVoteRequest request, CancellationToken ct)
        => Ok(await _votingService.CastVoteAsync(pointId, request, ct));

    [HttpDelete]
    public async Task<IActionResult> Remove(Guid pointId, CancellationToken ct)
        => Ok(await _votingService.RemoveVoteAsync(pointId, ct));

    [HttpPost("close")]
    public async Task<IActionResult> Close(Guid pointId, CancellationToken ct)
        => Ok(await _votingService.CloseVotingAsync(pointId, ct));
}
