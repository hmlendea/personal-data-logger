using PersonalDataLogger.Service.Models;

namespace PersonalDataLogger.Service.Processors
{
    public interface IAliExpressProcessor
    {
        void ProcessEmail(AvailableEmail email);
    }
}
