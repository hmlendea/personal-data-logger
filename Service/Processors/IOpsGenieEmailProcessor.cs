using PersonalDataLogger.Service.Models;

namespace PersonalDataLogger.Service.Processors
{
    public interface IOpsGenieEmailProcessor
    {
        void ProcessEmail(AvailableEmail email);
    }
}
