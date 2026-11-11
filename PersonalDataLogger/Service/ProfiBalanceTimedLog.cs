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
            => new ScheduledTimes(settings.ScheduledHours).GetNextExecution(currentTime);

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