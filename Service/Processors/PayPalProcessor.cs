using System.Collections.Generic;
using System.Text.RegularExpressions;
using PersonalDataLogger.Client;
using PersonalDataLogger.Service.Models;

namespace PersonalDataLogger.Service.Processors
{
    public class PayPalProcessor(
        IPersonalLogManagerService personalLogManagerService)
        : IPayPalProcessor
    {
        const string PlatformName = "PayPal";

        public void ProcessEmail(AvailableEmail email)
        {
            if (email.Subject.Contains("Conectare de pe un dispozitiv nou"))
            {
                Match emailAddressMatch = Regex.Match(
                    email.Body,
                    @"contul tău PayPal, (?<username>[^:]+):",
                    RegexOptions.IgnoreCase);

                string emailAddress = emailAddressMatch.Success
                    ? emailAddressMatch.Groups["username"].Value
                    : string.Empty;

                personalLogManagerService.SendPersonalLogToManager(
                    email.Timestamp,
                    "AccountLogin",
                    new Dictionary<string, string>()
                    {
                        ["platform"] = PlatformName,
                        ["username"] = emailAddress
                    });
            }
        }
    }
}
