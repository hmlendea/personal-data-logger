using System.Collections.Generic;
using System.Text.RegularExpressions;
using PersonalDataLogger.Client;
using PersonalDataLogger.Service.Models;

namespace PersonalDataLogger.Service.Processors
{
    public class GandiProcessor(
        IPersonalLogManagerService personalLogManagerService)
        : IGandiProcessor
    {
        const string PlatformName = "Gandi";

        public void ProcessEmail(AvailableEmail email)
        {
            if (email.Subject.Contains("connection on a new device"))
            {
                // Username (inside <b>...</b>)
                Match usernameMatch = Regex.Match(
                    email.Body,
                    @"username\s*<b>(?<username>[^<]+)</b>",
                    RegexOptions.IgnoreCase);

                string username = usernameMatch.Success
                    ? usernameMatch.Groups["username"].Value
                    : string.Empty;

                // IP Address
                Match ipMatch = Regex.Match(
                    email.Body,
                    @"IP address:\s*(?<ip>\d{1,3}(\.\d{1,3}){3})",
                    RegexOptions.IgnoreCase);

                string ipAddress = ipMatch.Success
                    ? ipMatch.Groups["ip"].Value
                    : string.Empty;

                personalLogManagerService.SendPersonalLogToManager(
                    email.Timestamp,
                    "AccountLogin",
                    new Dictionary<string, string>()
                    {
                        ["platform"] = PlatformName,
                        ["username"] = username,
                        ["ip_address"] = ipAddress
                    });
            }
        }
    }
}
