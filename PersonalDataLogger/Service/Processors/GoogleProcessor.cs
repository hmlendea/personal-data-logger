using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using PersonalDataLogger.Client;
using PersonalDataLogger.Service.Models;

namespace PersonalDataLogger.Service.Processors
{
    public class GoogleProcessor(
        IPersonalLogManagerService personalLogManagerService)
        : IGoogleProcessor
    {
        const string PlatformName = "Google";
        const string EnglishSecurityAlertSubject = "security alert";
        const string RomanianSecurityAlertSubject = "alertă de securitate";

        public void ProcessEmail(AvailableEmail email)
        {
            if (email.Subject.Contains(EnglishSecurityAlertSubject, StringComparison.OrdinalIgnoreCase) ||
                email.Subject.Contains(RomanianSecurityAlertSubject, StringComparison.OrdinalIgnoreCase))
            {
                Match usernameMatch = Regex.Match(
                    email.Body,
                    @"(?<username>[\w\-\.]+)@gmail\.com",
                    RegexOptions.IgnoreCase);

                string username = usernameMatch.Success
                    ? usernameMatch.Groups["username"].Value.Trim()
                    : string.Empty;

                personalLogManagerService.SendPersonalLogToManager(
                    email.Timestamp,
                    "AccountLogin",
                    new Dictionary<string, string>()
                    {
                        ["platform"] = PlatformName,
                        ["username"] = username
                    });
            }
        }
    }
}