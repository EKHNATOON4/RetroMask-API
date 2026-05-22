using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetroMask.Application.Dtos.ActionItems;
using RetroMask.Application.Services.ActionItems;

namespace RetroMask.API.Controllers.Common;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ActionItemsController : ControllerBase
{
    private readonly IActionItemService _service;

    public ActionItemsController(IActionItemService service)
    {
        _service = service;
    }

    [HttpGet("mine")]
    public async Task<IActionResult> GetMine([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await _service.GetMyActionItemsAsync(page, pageSize, ct));

    [HttpGet("session/{sessionId:guid}")]
    public async Task<IActionResult> GetBySession(Guid sessionId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await _service.GetSessionActionItemsAsync(sessionId, page, pageSize, ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => Ok(await _service.GetByIdAsync(id, ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateActionItemRequest request, CancellationToken ct)
    {
        var result = await _service.CreateAsync(request, ct);
        return result.Success ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result) : BadRequest(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateActionItemRequest request, CancellationToken ct)
        => Ok(await _service.UpdateAsync(id, request, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        => Ok(await _service.DeleteAsync(id, ct));

    [HttpPost("{id:guid}/updates")]
    public async Task<IActionResult> AddUpdate(Guid id, [FromBody] AddActionItemUpdateRequest request, CancellationToken ct)
        => Ok(await _service.AddUpdateAsync(id, request, ct));
}
