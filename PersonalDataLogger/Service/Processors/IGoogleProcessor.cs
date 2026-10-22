using PersonalDataLogger.Service.Models;

namespace PersonalDataLogger.Service.Processors
{
    public interface IGoogleProcessor
    {
        void ProcessEmail(AvailableEmail email);
    }
}