using CampusLostAndFound.Commands;
using CampusLostAndFound.Dtos;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CampusLostAndFound.Controllers;

[ApiController]
[Route("api/listings")]
public class ListingsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int limit = 9,
        [FromQuery] string? type = null,
        [FromQuery] string? search = null)
        => Ok(await mediator.Send(new GetListingsPagedQuery(page, limit, type, search)));

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var res = await mediator.Send(new GetListingByIdQuery(id));
        return res == null ? NotFound() : Ok(res);
    }

    [HttpPost]
    public async Task<IActionResult> Create(ListingCreateDto dto)
    {
        var res = await mediator.Send(new CreateListingCommand(dto));
        return CreatedAtAction(nameof(Get), new { id = res.Id }, res);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, ListingUpdateDto dto)
    {
        var res = await mediator.Send(new UpdateListingCommand(id, dto));
        return res == null ? NotFound() : Ok(res);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
        => await mediator.Send(new DeleteListingCommand(id)) ? NoContent() : NotFound();

    [HttpPatch("{id}/ai-metadata")]
    public async Task<IActionResult> UpdateAiMetadata(Guid id, ListingAiMetadataDto dto)
    {
        var res = await mediator.Send(new UpdateListingAiMetadataCommand(id, dto));
        return res == null ? NotFound() : Ok(res);
    }

    [HttpGet("report")]
    public async Task<IActionResult> GetReport([FromQuery] int year, [FromQuery] int month)
    {
        if (month < 1 || month > 12) return BadRequest("Invalid month");
        return Ok(await mediator.Send(new GetMonthlyReportQuery(year, month)));
    }
}
