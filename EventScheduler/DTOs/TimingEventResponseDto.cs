namespace EventScheduler.DTOs
{
    public class TimingEventResponseDto
    {
        public int Id { get; set; }
        public string TriggerSignalId { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public double DurationMs { get; set; }
        public string DurationFormatted { get; set; } = string.Empty;
        public string EndSignalId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int? ClassificationId { get; set; }
                public string? ClassificationName  { get; set; }
    }
}
