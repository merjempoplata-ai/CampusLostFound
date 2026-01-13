using CampusLostAndFound.Dtos;
using CampusLostAndFound.Services;
using Microsoft.AspNetCore.Mvc;

namespace CampusLostAndFound.Controllers
{
    [ApiController]
    [Route("api/listings")]
    public class ListingsController(IListingService service) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int limit = 9,
            [FromQuery] string? type = null,
            [FromQuery] string? search = null)
        {
            return Ok(await service.GetAllAsync(page, limit, type, search));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var res = await service.GetByIdAsync(id);
            return res == null ? NotFound() : Ok(res);
        }

        [HttpPost]
        public async Task<IActionResult> Create(ListingCreateDto dto)
        {
            var res = await service.CreateAsync(dto);
            return CreatedAtAction(nameof(Get), new { id = res.Id }, res);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, ListingUpdateDto dto)
        {
            var res = await service.UpdateAsync(id, dto);
            return res == null ? NotFound() : Ok(res);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id) => await service.DeleteAsync(id) ? NoContent() : NotFound();
    }
}