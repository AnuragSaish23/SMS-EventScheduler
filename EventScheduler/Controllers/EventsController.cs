using Microsoft.AspNetCore.Mvc;
using EventScheduler.DTOs;
using EventScheduler.Services.Interfaces;

namespace EventScheduler.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EventsController : ControllerBase
    {
        private readonly ITimingEventService _service;
        private readonly ISignalProcessingEngine _engine;

        public EventsController(ITimingEventService service, ISignalProcessingEngine engine)
        {
            _service = service;
            _engine = engine;
        }

        // GET /api/events?from=2026-04-01&to=2026-04-30&page=1&pageSize=50
        [HttpGet]
        public async Task<IActionResult> GetEvents(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var events = await _service.GetEventsAsync(from, to, page, pageSize);
            return Ok(events);
        }

        // GET /api/events/active
        [HttpGet("active")]
        public IActionResult GetActiveTimer()
        {
            var status = _engine.GetStatus();
            return Ok(status);
        }
    }
}
