using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PersonalDataLogger.Client
{
    public interface IPersonalLogManagerService
    {
        Task SendPersonalLogToManager(
            DateTimeOffset timestamp,
            string template);

        Task SendPersonalLogToManager(
            DateTimeOffset timestamp,
            string template,
            Dictionary<string, string> data);

        Task SendPersonalLogToManager(
            string date,
            string time,
            string timeZone,
            string template);

        Task SendPersonalLogToManager(
            string date,
            string time,
            string timeZone,
            string template,
            Dictionary<string, string> data);
    }
}
