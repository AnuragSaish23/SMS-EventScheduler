namespace EventScheduler.DTOs
{
    public class SignalDataDto
    {
        public string Id { get; set; } = string.Empty;
        public List<SignalValueDto> Values { get; set; } = new();
    }

    public class SignalValueDto
    {
        public DateTime TimeStamp { get; set; }
        public bool Value { get; set; }
        public string QualityCode { get; set; } = string.Empty;
        public string QualityFlag { get; set; } = string.Empty;
    }
}
