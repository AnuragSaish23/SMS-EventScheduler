using Microsoft.AspNetCore.Mvc;
using EventScheduler.DTOs;
using EventScheduler.Services.Interfaces;

namespace EventScheduler.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClassificationController : ControllerBase
    {
        private readonly ITimingEventService _service;

        public ClassificationController(ITimingEventService service)
        {
            _service = service;
        }

        // GET /api/classification — flat list
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var classifications = await _service.GetAllClassificationsAsync();
            return Ok(classifications);
        }

        // GET /api/classification/tree — nested tree
        [HttpGet("tree")]
        public async Task<IActionResult> GetTree()
        {
            var all = await _service.GetAllClassificationsAsync();

            // Build tree: find roots, attach children
            var lookup = all.ToDictionary(c => c.Id);
            var roots = new List<ClassificationResponseDto>();

            foreach (var item in all)
            {
                if (item.ParentId == null)
                {
                    roots.Add(item);
                }
                else if (lookup.ContainsKey(item.ParentId.Value))
                {
                    lookup[item.ParentId.Value].Children.Add(item);
                }
            }

            return Ok(roots);
        }

        // POST /api/classification
        [HttpPost]
        public async Task<IActionResult> Add([FromBody] ClassificationCreateDto dto)
        {
            try
            {
                var result = await _service.AddClassificationAsync(dto);
                return CreatedAtAction(nameof(GetAll), new { id = result.Id }, result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // DELETE /api/classification/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _service.DeleteClassificationAsync(id);
            if (!deleted) return NotFound(new { error = "Classification not found" });
            return NoContent();
        }
        // PUT /api/classification/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] ClassificationCreateDto dto)
        {
            var updated = await _service.UpdateClassificationAsync(id, dto.Name);
            if (updated == null)
                return NotFound(new { error = "Classification not found" });
            return Ok(updated);
        }
    }
}
