using System;
using System.Threading.Tasks;

namespace PersonalDataLogger.Client
{
    public interface IPersonalLogManagerService
    {
        Task SendPersonalLogToManager(
            DateTimeOffset timestamp,
            string template);

        Task SendPersonalLogToManager(
            string date,
            string time,
            string timeZone,
            string template);
    }
}
