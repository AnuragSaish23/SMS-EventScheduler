using Microsoft.AspNetCore.Mvc;
using EventScheduler.DTOs;
using EventScheduler.Services.Interfaces;

namespace EventScheduler.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConfigController : ControllerBase
    {
        private readonly ITimingEventService _service;

        public ConfigController(ITimingEventService service)
        {
            _service = service;
        }

        // GET /api/config
        [HttpGet]
        public async Task<IActionResult> GetAllConfigs()
        {
            var configs = await _service.GetAllConfigsAsync();
            return Ok(configs);
        }

        // POST /api/config
        [HttpPost]
        public async Task<IActionResult> AddConfig([FromBody] SignalConfigCreateDto config)
        {
            if (string.IsNullOrWhiteSpace(config.SignalId))
            {
                return BadRequest("SignalId is required.");
            }

            if (config.SignalType != "StartTrigger" && config.SignalType != "EndTrigger")
            {
                return BadRequest("SignalType must be 'StartTrigger' or 'EndTrigger'.");
            }

            var result = await _service.AddConfigAsync(config);
            return Created($"/api/config/{result.Id}", result);
        }

        // DELETE /api/config/3
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteConfig(int id)
        {
            var deleted = await _service.DeleteConfigAsync(id);

            if (!deleted)
            {
                return NotFound($"Config with Id {id} not found.");
            }

            return NoContent();
        }
    }
}
