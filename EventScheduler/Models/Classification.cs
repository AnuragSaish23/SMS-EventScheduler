using System.ComponentModel.DataAnnotations;

namespace EventScheduler.Models
{
    public class Classification
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        // NULL = root node, otherwise points to parent's Id
        public int? ParentId { get; set; }

        // 1 = root, 2 = sub-category, 3 = specific reason
        [Required]
        public int Level { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
