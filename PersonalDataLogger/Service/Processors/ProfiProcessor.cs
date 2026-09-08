using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using PersonalDataLogger.Client;
using PersonalDataLogger.Service.Models;

namespace PersonalDataLogger.Service.Processors
{
    public class ProfiProcessor(
        IPersonalLogManagerService personalLogManagerService)
        : IProfiProcessor
    {
        const string PlatformName = "Profi";

        public void ProcessEmail(AvailableEmail email)
        {
            if (email.Subject.Contains("Profi Prize Won", StringComparison.OrdinalIgnoreCase))
            {
                Match accountIdMatch = Regex.Match(
                    email.Body,
                    @"Account:\s*.*?\((?<account_id>\d+)\)",
                    RegexOptions.IgnoreCase);

                string accountId = accountIdMatch.Success
                    ? accountIdMatch.Groups["account_id"].Value
                    : string.Empty;

                Match prizeMatch = Regex.Match(
                    email.Body,
                    @"Item:\s*(?<prize>.+)",
                    RegexOptions.IgnoreCase);

                string prize = prizeMatch.Success
                    ? prizeMatch.Groups["prize"].Value.Trim()
                    : string.Empty;

                personalLogManagerService.SendPersonalLogToManager(
                    email.Timestamp,
                    "BotPrizeWinning",
                    new Dictionary<string, string>()
                    {
                        ["platform"] = PlatformName,
                        ["account_id"] = accountId,
                        ["prize_description"] = prize
                    });
            }
        }
    }
}
