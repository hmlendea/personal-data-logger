using System.Collections.Generic;
using PersonalDataLogger.Client;
using PersonalDataLogger.Configuration;
using PersonalDataLogger.Service.Models;

namespace PersonalDataLogger.Service.Processors
{
    public class AliExpressProcessor(
        IPersonalLogManagerService personalLogManagerService,
        AliExpressSettings settings)
        : IAliExpressProcessor
    {
        const string PlatformName = "AliExpress";

        public void ProcessEmail(AvailableEmail email)
        {
            if (email.Subject.Contains("Your AliExpress verification code"))
            {
                personalLogManagerService.SendPersonalLogToManager(
                    email.Timestamp,
                    "AccountLogin",
                    new Dictionary<string, string>()
                    {
                        ["platform"] = PlatformName,
                        ["email_address"] = settings.EmailAddress
                    });
            }
        }
    }
}
