using System.ComponentModel.DataAnnotations;

namespace EventScheduler.DTOs
{
    public class ClassificationCreateDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        // NULL = root node, otherwise the parent's Id
        public int? ParentId { get; set; }
    }
}
