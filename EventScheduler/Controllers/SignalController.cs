using Microsoft.AspNetCore.Mvc;
using EventScheduler.DTOs;
using EventScheduler.Services.Interfaces;

namespace EventScheduler.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SignalsController : ControllerBase
    {
        private readonly ISignalProcessingEngine _engine;

        public SignalsController(ISignalProcessingEngine engine)
        {
            _engine = engine;
        }

        // POST /api/signals
        [HttpPost]
        public IActionResult ProcessSignals([FromBody] List<SignalDataDto> signals)
        {
            if (signals == null || signals.Count == 0)
            {
                return BadRequest("No signal data provided.");
            }

            var results = _engine.ProcessSignals(signals);
            return Ok(results);
        }
    }
}
