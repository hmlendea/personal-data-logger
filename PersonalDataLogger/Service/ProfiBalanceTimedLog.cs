using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

using NuciLog.Core;

using PersonalDataLogger.Client;
using PersonalDataLogger.Configuration;
using PersonalDataLogger.Logging;

namespace PersonalDataLogger.Service
{
    public sealed class ProfiBalanceTimedLog(
        IProfiAccountsService profiAccountsService,
        IPersonalLogManagerService personalLogManagerService,
        ProfiBotServerSettings settings,
        ILogger logger)
        : ITimedLog
    {
        private static string AmountKey => "amount";
        private static string AccountKey => "account";
        private static string CurrencyCode => "RON";
        private static string CurrencyKey => "currency";
        private static string PlatformKey => "platform";
        private static string PlatformName => "Profi Bot Server";
        private static string TemplateName => "BotsTotalBalanceMeasurement";

        public DateTimeOffset GetNextExecution(DateTimeOffset currentTime)
        {
            DateTime localCurrentTime = currentTime.LocalDateTime;
            List<TimeSpan> scheduledHours = GetScheduledHours();
            DateTime nextExecution = localCurrentTime.Date.Add(scheduledHours[0]);

            foreach (TimeSpan scheduledHour in scheduledHours)
            {
                DateTime candidate = localCurrentTime.Date.Add(scheduledHour);

                if (candidate > localCurrentTime)
                {
                    nextExecution = candidate;
                    break;
                }
            }

            if (nextExecution <= localCurrentTime)
            {
                nextExecution = nextExecution.AddDays(1);
            }

            TimeSpan localOffset = TimeZoneInfo.Local.GetUtcOffset(nextExecution);

            return new DateTimeOffset(nextExecution, localOffset);
        }

        private List<TimeSpan> GetScheduledHours()
        {
            if (string.IsNullOrWhiteSpace(settings.ScheduledHours))
            {
                throw new FormatException(
                    "The Profi scheduled hours configuration is empty. Use one or more times in the HH:mm format.");
            }

            string[] configuredTimes = settings.ScheduledHours.Split(',', StringSplitOptions.TrimEntries);
            List<TimeSpan> scheduledHours = [];

            foreach (string configuredTime in configuredTimes)
            {
                if (!TimeSpan.TryParseExact(
                    configuredTime,
                    ["h\\:mm", "hh\\:mm"],
                    CultureInfo.InvariantCulture,
                    out TimeSpan balanceTime) ||
                    balanceTime < TimeSpan.Zero ||
                    balanceTime >= TimeSpan.FromDays(1))
                {
                    throw new FormatException(
                        $"The Profi balance time '{configuredTime}' is invalid. Use the HH:mm format.");
                }

                scheduledHours.Add(balanceTime);
            }

            scheduledHours.Sort();

            return scheduledHours;
        }

        public async Task Execute()
        {
            logger.Info(
                MyOperation.ExecuteTimedLog,
                OperationStatus.Started,
                new LogInfo(MyLogInfoKey.Template, TemplateName));

            decimal balance = await profiAccountsService.GetEnabledAccountsBalance();
            Dictionary<string, string> data = new()
            {
                [PlatformKey] = PlatformName,
                [AccountKey] = settings.AccountName,
                [AmountKey] = balance.ToString(CultureInfo.InvariantCulture),
                [CurrencyKey] = CurrencyCode
            };

            await personalLogManagerService.SendPersonalLogToManager(
                DateTimeOffset.Now,
                TemplateName,
                data);

            logger.Info(
                MyOperation.ExecuteTimedLog,
                OperationStatus.Success,
                new LogInfo(MyLogInfoKey.Template, TemplateName),
                new LogInfo(MyLogInfoKey.Amount, balance));
        }
    }
}