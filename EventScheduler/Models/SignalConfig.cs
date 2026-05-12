using System.ComponentModel.DataAnnotations;

namespace EventScheduler.Models
{
    public class SignalConfig
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string SignalId { get; set; } = string.Empty;

        [Required]
        public string SignalType { get; set; } = string.Empty;  // "StartTrigger" or "EndTrigger"

        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int? ClassificationId { get; set; }
    }
}
