using CampusLostAndFound.Dtos;
using CampusLostAndFound.Services;
using Microsoft.AspNetCore.Mvc;

namespace CampusLostAndFound.Controllers
{
    [ApiController]
    [Route("claims")]
    public class ClaimsController(IClaimService service) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await service.GetAllAsync());

        [HttpPost("/listings/{listingId}/claims")]
        public async Task<IActionResult> Create(Guid listingId, ClaimCreateDto dto)
        {
            var res = await service.SubmitAsync(listingId, dto);
            return res == null ? NotFound() : Created("", res);
        }

        [HttpPost("{id}/accept")]
        public async Task<IActionResult> Accept(Guid id) => await service.AcceptAsync(id) ? Ok() : NotFound();

        [HttpPost("{id}/reject")]
        public async Task<IActionResult> Reject(Guid id) => await service.RejectAsync(id) ? Ok() : NotFound();
    }
}