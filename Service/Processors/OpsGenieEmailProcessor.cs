using NuciLog.Core;
using PersonalDataLogger.Service.Models;

namespace PersonalDataLogger.Service.Processors
{
    public class OpsGenieEmailProcessor(ILogger logger) : IOpsGenieEmailProcessor
    {
        public void ProcessEmail(AvailableEmail email)
        {
        }
    }
}
