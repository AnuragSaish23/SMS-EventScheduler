using System.ComponentModel.DataAnnotations;

namespace EventScheduler.Models
{
    public class TimingEvent
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string TriggerSignalId { get; set; } = string.Empty;

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public double DurationMs { get; set; }

        public string DurationFormatted { get; set; } = string.Empty;

        [Required]
        public string EndSignalId { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int? ClassificationId { get; set; }
    }
}
