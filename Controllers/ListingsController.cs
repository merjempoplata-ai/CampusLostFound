using CampusLostAndFound.Dtos;
using CampusLostAndFound.Services;
using Microsoft.AspNetCore.Mvc;

namespace CampusLostAndFound.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ListingsController(IListingService service) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetAll(int page = 1, int pageSize = 10) => Ok(await service.GetAllAsync(page, pageSize));

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