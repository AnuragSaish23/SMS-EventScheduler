using Microsoft.EntityFrameworkCore;
using EventScheduler.Data;
using EventScheduler.DTOs;
using EventScheduler.Models;
using EventScheduler.Services.Interfaces;

namespace EventScheduler.Services
{
    public class TimingEventService : ITimingEventService
    {
        private readonly AppDbContext _context;

        public TimingEventService(AppDbContext context)
        {
            _context = context;
        }

        // ==================== TIMING EVENTS ====================

        public async Task<List<TimingEventResponseDto>> GetEventsAsync(
            DateTime? from, DateTime? to, int page, int pageSize)
        {
            var offset = (page - 1) * pageSize;

            List<TimingEvent> events;

            if (from.HasValue && to.HasValue)
            {
                events = await _context.TimingEvents
                    .FromSqlInterpolated(
                        $@"SELECT * FROM ""TimingEvents"" 
                           WHERE ""StartTime"" >= {from.Value} 
                           AND ""EndTime"" <= {to.Value} 
                           ORDER BY ""CreatedAt"" DESC 
                           LIMIT {pageSize} OFFSET {offset}")
                    .ToListAsync();
            }
            else if (from.HasValue)
            {
                events = await _context.TimingEvents
                    .FromSqlInterpolated(
                        $@"SELECT * FROM ""TimingEvents"" 
                           WHERE ""StartTime"" >= {from.Value} 
                           ORDER BY ""CreatedAt"" DESC 
                           LIMIT {pageSize} OFFSET {offset}")
                    .ToListAsync();
            }
            else if (to.HasValue)
            {
                events = await _context.TimingEvents
                    .FromSqlInterpolated(
                        $@"SELECT * FROM ""TimingEvents"" 
                           WHERE ""EndTime"" <= {to.Value} 
                           ORDER BY ""CreatedAt"" DESC 
                           LIMIT {pageSize} OFFSET {offset}")
                    .ToListAsync();
            }
            else
            {
                events = await _context.TimingEvents
                    .FromSqlInterpolated(
                        $@"SELECT * FROM ""TimingEvents"" 
                           ORDER BY ""CreatedAt"" DESC 
                           LIMIT {pageSize} OFFSET {offset}")
                    .ToListAsync();
            }

            var result = new List<TimingEventResponseDto>();

            foreach (var e in events)
            {
                string? classificationName = null;
                if (e.ClassificationId.HasValue)
                {
                    var classification = await _context.Classifications
                        .FromSqlInterpolated(
                            $@"SELECT * FROM ""Classifications"" WHERE ""Id"" = {e.ClassificationId.Value}")
                        .FirstOrDefaultAsync();
                    classificationName = classification?.Name;
                }

                result.Add(new TimingEventResponseDto
                {
                    Id = e.Id,
                    TriggerSignalId = e.TriggerSignalId,
                    StartTime = e.StartTime,
                    EndTime = e.EndTime,
                    DurationMs = e.DurationMs,
                    DurationFormatted = e.DurationFormatted,
                    EndSignalId = e.EndSignalId,
                    ClassificationId = e.ClassificationId,
                    ClassificationName = classificationName,
                    CreatedAt = e.CreatedAt
                });
            }

            return result;
        }


        public async Task SaveTimingEventAsync(TimingEvent timingEvent)
        {
            await _context.Database.ExecuteSqlInterpolatedAsync(
                    $@"INSERT INTO ""TimingEvents"" 
                   (""TriggerSignalId"", ""StartTime"", ""EndTime"", ""DurationMs"", ""DurationFormatted"", ""EndSignalId"", ""ClassificationId"", ""CreatedAt"")
                   VALUES 
                   ({timingEvent.TriggerSignalId}, {timingEvent.StartTime}, {timingEvent.EndTime}, 
                    {timingEvent.DurationMs}, {timingEvent.DurationFormatted}, {timingEvent.EndSignalId}, 
                    {timingEvent.ClassificationId}, {timingEvent.CreatedAt})");

        }

        // ==================== SIGNAL CONFIG ====================

        public async Task<List<SignalConfigResponseDto>> GetAllConfigsAsync()
        {
            var configs = await _context.SignalConfigs
                .FromSqlInterpolated($@"SELECT * FROM ""SignalConfigs"" ORDER BY ""CreatedAt"" DESC")
                .ToListAsync();

            var result = new List<SignalConfigResponseDto>();

            foreach (var c in configs)
            {
                string? classificationName = null;
                if (c.ClassificationId.HasValue)
                {
                    var classification = await _context.Classifications
                        .FromSqlInterpolated(
                            $@"SELECT * FROM ""Classifications"" WHERE ""Id"" = {c.ClassificationId.Value}")
                        .FirstOrDefaultAsync();
                    classificationName = classification?.Name;
                }

                result.Add(new SignalConfigResponseDto
                {
                    Id = c.Id,
                    SignalId = c.SignalId,
                    SignalType = c.SignalType,
                    Description = c.Description,
                    ClassificationId = c.ClassificationId,
                    ClassificationName = classificationName,
                    CreatedAt = c.CreatedAt
                });
            }

            return result;
        }

        public async Task<SignalConfigResponseDto> AddConfigAsync(SignalConfigCreateDto config)
        {
            var createdAt = DateTime.UtcNow;

            await _context.Database.ExecuteSqlInterpolatedAsync(
                $@"INSERT INTO ""SignalConfigs"" (""SignalId"", ""SignalType"", ""Description"", ""ClassificationId"", ""CreatedAt"")
                   VALUES ({config.SignalId}, {config.SignalType}, {config.Description}, {config.ClassificationId}, {createdAt})");

            // Get the last inserted record
            var entity = await _context.SignalConfigs
                .FromSqlInterpolated(
                    $@"SELECT * FROM ""SignalConfigs"" 
                       WHERE ""SignalId"" = {config.SignalId} 
                       ORDER BY ""Id"" DESC LIMIT 1")
                .FirstOrDefaultAsync();

            // Look up classification name if linked
            string? classificationName = null;
            if (entity!.ClassificationId.HasValue)
            {
                var classification = await _context.Classifications
                    .FromSqlInterpolated(
                        $@"SELECT * FROM ""Classifications"" WHERE ""Id"" = {entity.ClassificationId.Value}")
                    .FirstOrDefaultAsync();
                classificationName = classification?.Name;
            }

            return new SignalConfigResponseDto
            {
                Id = entity.Id,
                SignalId = entity.SignalId,
                SignalType = entity.SignalType,
                Description = entity.Description,
                ClassificationId = entity.ClassificationId,
                ClassificationName = classificationName,
                CreatedAt = entity.CreatedAt
            };
        }

        public async Task<bool> DeleteConfigAsync(int id)
        {
            var rowsAffected = await _context.Database.ExecuteSqlInterpolatedAsync(
                $@"DELETE FROM ""SignalConfigs"" WHERE ""Id"" = {id}");

            return rowsAffected > 0;
        }

        public async Task<List<SignalConfig>> GetConfigsByTypeAsync(string signalType)
        {
            return await _context.SignalConfigs
                .FromSqlInterpolated(
                    $@"SELECT * FROM ""SignalConfigs"" WHERE ""SignalType"" = {signalType}")
                .ToListAsync();
        }

        // ==================== RAW SIGNAL LOGGING ====================

        public async Task LogRawSignalAsync(RawSignalLog log)
        {
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $@"INSERT INTO ""RawSignalLogs"" 
                   (""SignalId"", ""TimeStamp"", ""Value"", ""QualityCode"", ""QualityFlag"", ""ReceivedAt"")
                   VALUES 
                   ({log.SignalId}, {log.TimeStamp}, {log.Value}, 
                    {log.QualityCode}, {log.QualityFlag}, {log.ReceivedAt})");
        }
        // ==================== CLASSIFICATIONS ====================

        public async Task<List<ClassificationResponseDto>> GetAllClassificationsAsync()
        {
            var classifications = await _context.Classifications
                .FromSqlInterpolated($@"SELECT * FROM ""Classifications"" ORDER BY ""Level"", ""Name""")
                .ToListAsync();

            return classifications.Select(c => new ClassificationResponseDto
            {
                Id = c.Id,
                Name = c.Name,
                ParentId = c.ParentId,
                Level = c.Level,
                CreatedAt = c.CreatedAt
            }).ToList();
        }

        public async Task<ClassificationResponseDto?> GetClassificationByIdAsync(int id)
        {
            var entity = await _context.Classifications
                .FromSqlInterpolated($@"SELECT * FROM ""Classifications"" WHERE ""Id"" = {id}")
                .FirstOrDefaultAsync();

            if (entity == null) return null;

            return new ClassificationResponseDto
            {
                Id = entity.Id,
                Name = entity.Name,
                ParentId = entity.ParentId,
                Level = entity.Level,
                CreatedAt = entity.CreatedAt
            };
        }

        public async Task<ClassificationResponseDto> AddClassificationAsync(ClassificationCreateDto dto)
        {
            int level = 1;

            // If it has a parent, calculate the level
            if (dto.ParentId.HasValue)
            {
                var parent = await _context.Classifications
                    .FromSqlInterpolated($@"SELECT * FROM ""Classifications"" WHERE ""Id"" = {dto.ParentId.Value}")
                    .FirstOrDefaultAsync();

                if (parent == null)
                    throw new ArgumentException("Parent classification not found.");

                level = parent.Level + 1;

                if (level > 3)
                    throw new ArgumentException("Maximum classification depth is 3 levels.");
            }

            var createdAt = DateTime.UtcNow;

            await _context.Database.ExecuteSqlInterpolatedAsync(
                $@"INSERT INTO ""Classifications"" (""Name"", ""ParentId"", ""Level"", ""CreatedAt"")
                   VALUES ({dto.Name}, {dto.ParentId}, {level}, {createdAt})");

            // Get the inserted record
            var entity = await _context.Classifications
                .FromSqlInterpolated(
                    $@"SELECT * FROM ""Classifications"" 
                       WHERE ""Name"" = {dto.Name} AND ""Level"" = {level}
                       ORDER BY ""Id"" DESC LIMIT 1")
                .FirstOrDefaultAsync();

            return new ClassificationResponseDto
            {
                Id = entity!.Id,
                Name = entity.Name,
                ParentId = entity.ParentId,
                Level = entity.Level,
                CreatedAt = entity.CreatedAt
            };
        }

        public async Task<bool> DeleteClassificationAsync(int classificationId)
        {
            // Delete children first (all descendants), then the node itself
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $@"DELETE FROM ""Classifications"" WHERE ""ParentId"" IN 
                   (SELECT ""Id"" FROM ""Classifications"" WHERE ""ParentId"" = {classificationId})");

            await _context.Database.ExecuteSqlInterpolatedAsync(
                $@"DELETE FROM ""Classifications"" WHERE ""ParentId"" = {classificationId}");

            var rowsAffected = await _context.Database.ExecuteSqlInterpolatedAsync(
                $@"DELETE FROM ""Classifications"" WHERE ""Id"" = {classificationId}");

            return rowsAffected > 0;
        }

    }
}
