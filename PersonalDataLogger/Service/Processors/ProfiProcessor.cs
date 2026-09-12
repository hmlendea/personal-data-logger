using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using PersonalDataLogger.Client;
using PersonalDataLogger.Service.Models;

namespace PersonalDataLogger.Service.Processors
{
    public sealed class ProfiProcessor(
        IPersonalLogManagerService personalLogManagerService)
        : IProfiProcessor
    {
        private static readonly Regex AccountIdRegex = new(
            @"(?:Account|Cont):\s*.*?\((?<account_id>\d+)\)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex PrizeRegex = new(
            @"(?:Item|Articol):\s*(?<prize>.+)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static string EnglishPrizeSubject => "Profi Prize Won";

        private static string RomanianPrizeSubject => "Ai câștigat un premiu Profi!";

        private static string BotPrizeLogName => "BotPrizeWinning";

        private static string PlatformName => "Profi";

        private static string PlatformKey => "platform";

        private static string AccountIdKey => "account_id";

        private static string PrizeDescriptionKey => "prize_description";

        public void ProcessEmail(AvailableEmail email)
        {
            if (!IsPrizeEmail(email.Subject))
            {
                return;
            }

            Match accountIdMatch = AccountIdRegex.Match(email.Body);
            string accountId = string.Empty;

            if (accountIdMatch.Success)
            {
                accountId = accountIdMatch.Groups[AccountIdKey].Value;
            }

            Match prizeMatch = PrizeRegex.Match(email.Body);
            string prize = string.Empty;

            if (prizeMatch.Success)
            {
                prize = prizeMatch.Groups["prize"].Value.Trim();
            }

            personalLogManagerService.SendPersonalLogToManager(
                email.Timestamp,
                BotPrizeLogName,
                new Dictionary<string, string>()
                {
                    [PlatformKey] = PlatformName,
                    [AccountIdKey] = accountId,
                    [PrizeDescriptionKey] = prize
                });
        }

        private static bool IsPrizeEmail(string subject)
            => subject.Contains(EnglishPrizeSubject, StringComparison.OrdinalIgnoreCase)
                || subject.Contains(RomanianPrizeSubject, StringComparison.OrdinalIgnoreCase);
    }
}
