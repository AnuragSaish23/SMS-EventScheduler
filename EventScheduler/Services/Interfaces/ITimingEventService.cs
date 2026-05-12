using EventScheduler.DTOs;
using EventScheduler.Models;

namespace EventScheduler.Services.Interfaces
{
    public interface ITimingEventService
    {
        // Timing Events
        Task<List<TimingEventResponseDto>> GetEventsAsync(DateTime? from, DateTime? to, int page, int pageSize);
        Task SaveTimingEventAsync(TimingEvent timingEvent);

        // Signal Config
        Task<List<SignalConfigResponseDto>> GetAllConfigsAsync();
        Task<SignalConfigResponseDto> AddConfigAsync(SignalConfigCreateDto config);
        Task<bool> DeleteConfigAsync(int id);
        Task<List<SignalConfig>> GetConfigsByTypeAsync(string signalType);

        // Raw Signal Logging
        Task LogRawSignalAsync(RawSignalLog log);

        // ==================== CLASSIFICATIONS ====================
        Task<List<ClassificationResponseDto>> GetAllClassificationsAsync();
        Task<ClassificationResponseDto?> GetClassificationByIdAsync(int id);
        Task<ClassificationResponseDto> AddClassificationAsync(ClassificationCreateDto dto);
        Task<bool> DeleteClassificationAsync(int classificationId);
    }
}
