using CampusLostAndFound.Commands;
using CampusLostAndFound.Dtos;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CampusLostAndFound.Controllers;

[ApiController]
[Route("api/ai")]
public class AiController(IMediator mediator) : ControllerBase
{
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string? query)
    {
        if (string.IsNullOrWhiteSpace(query)) return BadRequest("query parameter is required.");
        var result = await mediator.Send(new RagSearchQuery(query));
        return Ok(new { matches = result.Listings, answer = result.Answer, citations = result.Citations });
    }

    [HttpPost("reindex/listings")]
    public async Task<IActionResult> ReindexListings()
    {
        var count = await mediator.Send(new ReindexListingsCommand());
        return Ok(new { indexed = count });
    }

    [HttpGet("similar/{listingId:guid}")]
    public async Task<IActionResult> Similar(Guid listingId, [FromQuery] int k = 6)
    {
        if (k < 1 || k > 50) return BadRequest("k must be between 1 and 50.");
        return Ok(await mediator.Send(new SimilarListingsQuery(listingId, k)));
    }

    [HttpPost("claim-check")]
    public async Task<IActionResult> ClaimCheck([FromBody] ClaimCheckRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.ClaimMessage)) return BadRequest("claimMessage is required.");
        try
        {
            return Ok(await mediator.Send(new ClaimCheckCommand(dto)));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPost("moderation/analyze")]
    public async Task<IActionResult> ModerationAnalyze([FromBody] ModerationRequestDto dto)
        => Ok(await mediator.Send(new ModerationAnalyzeCommand(dto)));

    [HttpGet("faq")]
    public async Task<IActionResult> Faq([FromQuery] int days = 30)
    {
        if (days < 1 || days > 365) return BadRequest("days must be between 1 and 365.");
        return Ok(await mediator.Send(new FaqQuery(days)));
    }

    [HttpPost("assist")]
    public async Task<IActionResult> Assist([FromBody] AssistRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Message)) return BadRequest("message is required.");
        return Ok(await mediator.Send(new AiAssistQuery(dto.Message)));
    }
}
