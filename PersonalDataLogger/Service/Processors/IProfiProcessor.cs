using PersonalDataLogger.Service.Models;

namespace PersonalDataLogger.Service.Processors
{
    public interface IProfiProcessor
    {
        void ProcessEmail(AvailableEmail email);
    }
}
