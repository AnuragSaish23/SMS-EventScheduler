using EventScheduler.DTOs;
using EventScheduler.Models;
using EventScheduler.Services.Interfaces;

namespace EventScheduler.Services
{
    public class SignalProcessingEngine : ISignalProcessingEngine
    {
        // Dependencies
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SignalProcessingEngine> _logger;

        // State - these persist because this is a Singleton
        private readonly object _lockObject = new();
        private bool _isTimerRunning = false;
        private DateTime _timerStartTime;
        private string _triggerSignalId = string.Empty;
        private int? _classificationId = null;

        private readonly IInfluxDbService _influxDb;

        public SignalProcessingEngine(
            IServiceScopeFactory scopeFactory,
            ILogger<SignalProcessingEngine> logger,
            IInfluxDbService influxDb)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _influxDb = influxDb;
        }

        public List<SignalProcessingResultDto> ProcessSignals(List<SignalDataDto> signals)
        {
            var results = new List<SignalProcessingResultDto>();

            foreach (var signal in signals)
            {
                foreach (var value in signal.Values)
                {
                    var result = ProcessSingleSignal(signal.Id, value);
                    results.Add(result);
                }
            }

            return results;
        }

        private SignalProcessingResultDto ProcessSingleSignal(string signalId, SignalValueDto value)
        {
            // Step 1: Log the raw signal to database
            LogRawSignal(signalId, value);

            // Step 2: We only care about TRUE values
            if (!value.Value)
            {
                return new SignalProcessingResultDto
                {
                    SignalId = signalId,
                    Action = "Logged",
                    Message = "Signal value is false — logged only."
                };
            }

            // Step 3: Check what type of signal this is
            var signalType = GetSignalType(signalId);

            if (signalType == null)
            {
                return new SignalProcessingResultDto
                {
                    SignalId = signalId,
                    Action = "Logged",
                    Message = "Signal is not configured as a trigger — logged only."
                };
            }

            // Step 4: Process based on signal type
            lock (_lockObject)
            {
                if (signalType == "StartTrigger")
                {
                    return HandleStartTrigger(signalId, value.TimeStamp);
                }
                else if (signalType == "EndTrigger")
                {
                    return HandleEndTrigger(signalId, value.TimeStamp);
                }
            }

            return new SignalProcessingResultDto
            {
                SignalId = signalId,
                Action = "Logged",
                Message = "Unknown signal type — logged only."
            };
        }

        private SignalProcessingResultDto HandleStartTrigger(string signalId, DateTime timeStamp)
        {
            // If timer is already running, ignore the duplicate
            if (_isTimerRunning)
            {
                _logger.LogInformation(
                    "Start signal '{SignalId}' ignored — timer already running since {StartTime}",
                    signalId, _timerStartTime);

                return new SignalProcessingResultDto
                {
                    SignalId = signalId,
                    Action = "Ignored",
                    Message = $"Timer already running since {_timerStartTime:O}. Duplicate start signal ignored.",
                    StartTime = _timerStartTime
                };
            }

            // Start the timer!
            _isTimerRunning = true;
            _timerStartTime = timeStamp;
            _triggerSignalId = signalId;
            _classificationId = GetClassificationId(signalId);

            _logger.LogInformation(
                "Timer STARTED by signal '{SignalId}' at {TimeStamp}",
                signalId, timeStamp);

            return new SignalProcessingResultDto
            {
                SignalId = signalId,
                Action = "TimerStarted",
                Message = $"Timer started at {timeStamp:O}",
                StartTime = timeStamp
            };
        }

        private SignalProcessingResultDto HandleEndTrigger(string signalId, DateTime timeStamp)
        {
            // If no timer is running, ignore
            if (!_isTimerRunning)
            {
                _logger.LogInformation(
                    "End signal '{SignalId}' ignored — no timer is running", signalId);

                return new SignalProcessingResultDto
                {
                    SignalId = signalId,
                    Action = "Ignored",
                    Message = "No timer is currently running. End signal ignored."
                };
            }

            // Calculate duration
            var duration = timeStamp - _timerStartTime;
            var durationMs = duration.TotalMilliseconds;
            var durationFormatted = duration.ToString(@"hh\:mm\:ss\.fff");

            // Save the timing event to database
            var timingEvent = new TimingEvent
            {
                TriggerSignalId = _triggerSignalId,
                StartTime = _timerStartTime,
                EndTime = timeStamp,
                DurationMs = durationMs,
                DurationFormatted = durationFormatted,
                EndSignalId = signalId,
                CreatedAt = DateTime.UtcNow,
                ClassificationId = _classificationId 
            };

            SaveTimingEvent(timingEvent);

            _logger.LogInformation(
                "Timer ENDED by signal '{SignalId}' at {TimeStamp}. Duration: {Duration}",
                signalId, timeStamp, durationFormatted);

            // Reset state for next cycle
            var result = new SignalProcessingResultDto
            {
                SignalId = signalId,
                Action = "TimerEnded",
                Message = $"Timer ended. Duration: {durationFormatted}",
                StartTime = _timerStartTime,
                EndTime = timeStamp,
                DurationMs = durationMs,
                DurationFormatted = durationFormatted
            };

            _isTimerRunning = false;
            _triggerSignalId = string.Empty;
            _classificationId = null;

            return result;
        }

        public EngineStatusDto GetStatus()
        {
            lock (_lockObject)
            {
                if (_isTimerRunning)
                {
                    var elapsed = DateTime.UtcNow - _timerStartTime;
                    return new EngineStatusDto
                    {
                        State = "Timing",
                        TimerStartedAt = _timerStartTime,
                        TriggerSignalId = _triggerSignalId,
                        ElapsedMs = elapsed.TotalMilliseconds,
                        ElapsedFormatted = elapsed.ToString(@"hh\:mm\:ss\.fff")
                    };
                }

                return new EngineStatusDto
                {
                    State = "Idle"
                };
            }
        }

        // Helper: Get signal type from database config
        private string? GetSignalType(string signalId)
        {
            using var scope = _scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<ITimingEventService>();
            var configs = service.GetConfigsByTypeAsync("StartTrigger").Result;
            var startMatch = configs.FirstOrDefault(c => c.SignalId == signalId);
            if (startMatch != null) return "StartTrigger";

            var endConfigs = service.GetConfigsByTypeAsync("EndTrigger").Result;
            var endMatch = endConfigs.FirstOrDefault(c => c.SignalId == signalId);
            if (endMatch != null) return "EndTrigger";

            return null;
        }

        // Helper: Log raw signal to database
        private void LogRawSignal(string signalId, SignalValueDto value)
        {
            using var scope = _scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<ITimingEventService>();
            var log = new RawSignalLog
            {
                SignalId = signalId,
                TimeStamp = value.TimeStamp,
                Value = value.Value,
                QualityCode = value.QualityCode,
                QualityFlag = value.QualityFlag,
                ReceivedAt = DateTime.UtcNow
            };

            // PostgreSQL write
            service.LogRawSignalAsync(log).Wait();

            // NEW: InfluxDB write
            _influxDb.WriteRawSignalAsync(log).Wait();
        }

        // Helper: Save timing event to database
        private void SaveTimingEvent(TimingEvent timingEvent)
        {
            using var scope = _scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<ITimingEventService>();

            // PostgreSQL write
            service.SaveTimingEventAsync(timingEvent).Wait();

            // NEW: InfluxDB write
            _influxDb.WriteTimingEventAsync(timingEvent).Wait();
        }
        // Helper: Get classification ID from signal config
        private int? GetClassificationId(string signalId)
        {
            using var scope = _scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<ITimingEventService>();
            var configs = service.GetConfigsByTypeAsync("StartTrigger").Result;
            var match = configs.FirstOrDefault(c => c.SignalId == signalId);
            return match?.ClassificationId;
        }

    }
}
