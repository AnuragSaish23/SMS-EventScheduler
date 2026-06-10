using EventScheduler.Models;

namespace EventScheduler.Services.Interfaces
{
	public interface IInfluxDbService
	{
		Task WriteRawSignalAsync(RawSignalLog log);
		Task WriteTimingEventAsync(TimingEvent timingEvent);
	}
}