namespace EventScheduler.DTOs
{
    public class SignalConfigCreateDto
    {
        public string SignalId { get; set; } = string.Empty;
        public string SignalType { get; set; } = string.Empty;  // "StartTrigger" or "EndTrigger"
        public string? Description { get; set; }
        public int? ClassificationId { get; set; }
    }

    public class SignalConfigResponseDto
    {
        public int Id { get; set; }
        public string SignalId { get; set; } = string.Empty;
        public string SignalType { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? ClassificationId { get; set; }
        public string? ClassificationName { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
