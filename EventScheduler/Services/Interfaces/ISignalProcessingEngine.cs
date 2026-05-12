using EventScheduler.DTOs;

namespace EventScheduler.Services.Interfaces
{
	public interface ISignalProcessingEngine
	{
		List<SignalProcessingResultDto> ProcessSignals(List<SignalDataDto> signals);
		EngineStatusDto GetStatus();
	}
}
