using PersonalDataLogger.Service.Models;

namespace PersonalDataLogger.Service.Processors
{
    public interface IPayPalProcessor
    {
        void ProcessEmail(AvailableEmail email);
    }
}
