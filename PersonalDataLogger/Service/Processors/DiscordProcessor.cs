using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using PersonalDataLogger.Client;
using PersonalDataLogger.Service.Models;

namespace PersonalDataLogger.Service.Processors
{
    public class DiscordProcessor(
        IPersonalLogManagerService personalLogManagerService)
        : IDiscordProcessor
    {
        const string PlatformName = "Discord";

        public void ProcessEmail(AvailableEmail email)
        {
            if (email.Subject.Contains(
                "verify discord login from new location",
                StringComparison.OrdinalIgnoreCase))
            {
                Match ipMatch = Regex.Match(
                    email.Body,
                    @"IP Address:\s*(?<ip>\d{1,3}(\.\d{1,3}){3})",
                    RegexOptions.IgnoreCase);

                string ipAddress = ipMatch.Success
                    ? ipMatch.Groups["ip"].Value
                    : string.Empty;

                Match locationMatch = Regex.Match(
                    email.Body,
                    @"Location:\s*(?<location>[^<]+)",
                    RegexOptions.IgnoreCase);

                string location = locationMatch.Success
                    ? locationMatch.Groups["location"].Value.Trim()
                    : string.Empty;

                personalLogManagerService.SendPersonalLogToManager(
                    email.Timestamp,
                    "AccountLogin",
                    new Dictionary<string, string>()
                    {
                        ["platform"] = PlatformName,
                        ["ip_address"] = ipAddress,
                        ["location"] = location
                    });
            }
        }
    }
}