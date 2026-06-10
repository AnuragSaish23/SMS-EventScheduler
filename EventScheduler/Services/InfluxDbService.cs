using EventScheduler.Models;
using EventScheduler.Services.Interfaces;
using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;

namespace EventScheduler.Services
{
    public class InfluxDbService : IInfluxDbService, IDisposable
    {
        private readonly InfluxDBClient _client;
        private readonly string _bucket;
        private readonly string _org;
        private readonly ILogger<InfluxDbService> _logger;

        public InfluxDbService(IConfiguration config, ILogger<InfluxDbService> logger)
        {
            _logger = logger;
            var url = config["InfluxDb:Url"] ?? "http://localhost:8086";
            var token = config["InfluxDb:Token"] ?? "sms-cmms-dev-token";
            _org = config["InfluxDb:Org"] ?? "sms-group";
            _bucket = config["InfluxDb:Bucket"] ?? "factory-signals";

            _client = new InfluxDBClient(url, token);
            _logger.LogInformation("InfluxDB client initialized → {Url}, bucket={Bucket}", url, _bucket);
        }

        public async Task WriteRawSignalAsync(RawSignalLog log)
        {
            try
            {
                var point = PointData
                    .Measurement("raw_signals")
                    .Tag("signalId", log.SignalId)
                    .Tag("qualityFlag", log.QualityFlag ?? "Good")
                    .Field("value", log.Value ? 1 : 0)
                    .Field("qualityCode", log.QualityCode ?? "192")
                    .Timestamp(log.TimeStamp.ToUniversalTime(), WritePrecision.Ms);

                using var writeApi = _client.GetWriteApi();
                writeApi.WritePoint(point, _bucket, _org);

                _logger.LogDebug("InfluxDB: wrote raw signal {SignalId}={Value}", log.SignalId, log.Value);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "InfluxDB write failed for raw signal {SignalId}. PostgreSQL still has the data.", log.SignalId);
            }
        }

        public async Task WriteTimingEventAsync(TimingEvent timingEvent)
        {
            try
            {
                var point = PointData
                    .Measurement("timing_events")
                    .Tag("triggerSignalId", timingEvent.TriggerSignalId)
                    .Tag("endSignalId", timingEvent.EndSignalId)
                    .Tag("classificationId", timingEvent.ClassificationId?.ToString() ?? "none")
                    .Field("durationMs", timingEvent.DurationMs)
                    .Field("durationFormatted", timingEvent.DurationFormatted)
                    .Timestamp(timingEvent.StartTime.ToUniversalTime(), WritePrecision.Ms);

                using var writeApi = _client.GetWriteApi();
                writeApi.WritePoint(point, _bucket, _org);

                _logger.LogInformation("InfluxDB: wrote timing event. Duration={Duration}", timingEvent.DurationFormatted);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "InfluxDB write failed for timing event. PostgreSQL still has the data.");
            }
        }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}