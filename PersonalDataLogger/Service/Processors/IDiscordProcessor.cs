using PersonalDataLogger.Service.Models;

namespace PersonalDataLogger.Service.Processors
{
    public interface IDiscordProcessor
    {
        void ProcessEmail(AvailableEmail email);
    }
}