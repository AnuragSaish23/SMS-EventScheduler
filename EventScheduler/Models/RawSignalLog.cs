using System.ComponentModel.DataAnnotations;

namespace EventScheduler.Models
{
    public class RawSignalLog
    {
        [Key]
        public long Id { get; set; }

        [Required]
        public string SignalId { get; set; } = string.Empty;

        public DateTime TimeStamp { get; set; }

        public bool Value { get; set; }

        public string QualityCode { get; set; } = string.Empty;

        public string QualityFlag { get; set; } = string.Empty;

        public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    }
}
