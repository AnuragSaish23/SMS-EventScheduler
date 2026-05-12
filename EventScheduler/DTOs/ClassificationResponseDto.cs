namespace EventScheduler.DTOs
{
    public class ClassificationResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? ParentId { get; set; }
        public int Level { get; set; }
        public DateTime CreatedAt { get; set; }

        // For tree response — children nested inside
        public List<ClassificationResponseDto> Children { get; set; } = new();
    }
}
