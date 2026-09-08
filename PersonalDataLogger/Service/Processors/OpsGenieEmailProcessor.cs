using System;
using System.Collections.Generic;
using PersonalDataLogger.Client;
using PersonalDataLogger.Configuration;
using PersonalDataLogger.Service.Models;

namespace PersonalDataLogger.Service.Processors
{
    public class OpsGenieEmailProcessor(
        IPersonalLogManagerService personalLogManagerService,
        PersonalSettings settings)
        : IOpsGenieEmailProcessor
    {
        public void ProcessEmail(AvailableEmail email)
        {
            if (email.Subject.StartsWith(
                "Your on-call rotation",
                StringComparison.OrdinalIgnoreCase))
            {
                if (email.Subject.EndsWith("is starting now", StringComparison.OrdinalIgnoreCase))
                {
                    personalLogManagerService.SendPersonalLogToManager(
                        email.Timestamp,
                        "WorkOnCallShiftBeginning",
                        new Dictionary<string, string>()
                        {
                            ["employer_name"] = settings.EmployerName
                        });
                }
                else if (email.Subject.EndsWith("is ending now", StringComparison.OrdinalIgnoreCase))
                {
                    personalLogManagerService.SendPersonalLogToManager(
                        email.Timestamp,
                        "WorkOnCallShiftEnding");
                }
            }
        }
    }
}
