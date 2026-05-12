namespace EventScheduler.DTOs
{
    public class EngineStatusDto
    {
        public string State { get; set; } = string.Empty;  // "Idle" or "Timing"
        public DateTime? TimerStartedAt { get; set; }
        public string? TriggerSignalId { get; set; }
        public double? ElapsedMs { get; set; }
        public string? ElapsedFormatted { get; set; }
    }
}
