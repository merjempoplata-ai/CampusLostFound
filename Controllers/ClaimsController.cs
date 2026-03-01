using CampusLostAndFound.Commands;
using CampusLostAndFound.Dtos;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CampusLostAndFound.Controllers;

[ApiController]
[Route("claims")]
public class ClaimsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await mediator.Send(new GetAllClaimsQuery()));

    [HttpPost("/listings/{listingId}/claims")]
    public async Task<IActionResult> Create(Guid listingId, ClaimCreateDto dto)
    {
        var res = await mediator.Send(new CreateClaimCommand(listingId, dto));
        return res == null ? NotFound() : Created("", res);
    }

    [HttpPost("{id}/accept")]
    public async Task<IActionResult> Accept(Guid id)
        => await mediator.Send(new AcceptClaimCommand(id)) ? Ok() : NotFound();

    [HttpPost("{id}/reject")]
    public async Task<IActionResult> Reject(Guid id)
        => await mediator.Send(new RejectClaimCommand(id)) ? Ok() : NotFound();
}
