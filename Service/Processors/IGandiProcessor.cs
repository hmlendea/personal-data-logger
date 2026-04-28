using PersonalDataLogger.Service.Models;

namespace PersonalDataLogger.Service.Processors
{
    public interface IGandiProcessor
    {
        void ProcessEmail(AvailableEmail email);
    }
}
