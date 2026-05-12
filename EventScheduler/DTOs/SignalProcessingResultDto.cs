namespace EventScheduler.DTOs
{
    public class SignalProcessingResultDto
    {
        public string SignalId { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;  // "TimerStarted", "TimerEnded", "Ignored", "Logged"
        public string Message { get; set; } = string.Empty;
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public double? DurationMs { get; set; }
        public string? DurationFormatted { get; set; }
    }
}
