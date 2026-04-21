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
            if (email.Subject.StartsWith("Your on-call rotation"))
            {
                if (email.Subject.EndsWith("is starting now"))
                {
                    personalLogManagerService.SendPersonalLogToManager(
                        email.Timestamp,
                        "WorkOnCallShiftBeginning",
                        new Dictionary<string, string>()
                        {
                            ["employer_name"] = settings.EmployerName
                        });
                }
                else if (email.Subject.EndsWith("is ending now"))
                {
                    personalLogManagerService.SendPersonalLogToManager(
                        email.Timestamp,
                        "WorkOnCallShiftEnding");
                }
            }
        }
    }
}
