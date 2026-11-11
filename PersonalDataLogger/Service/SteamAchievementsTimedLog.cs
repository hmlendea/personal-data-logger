using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using NuciLog.Core;

using PersonalDataLogger.Client;
using PersonalDataLogger.Configuration;
using PersonalDataLogger.Logging;

namespace PersonalDataLogger.Service
{
    public sealed class SteamAchievementsTimedLog(
        ISteamService steamService,
        IPersonalLogManagerService personalLogManagerService,
        SteamSettings settings,
        ILogger logger)
        : ITimedLog
    {
        private static string AchievementNameKey => "achievement_name";
        private static string GameNameKey => "game_name";
        private static string PlatformKey => "platform";
        private static string PlatformName => "Steam";
        private static string TemplateName => "GameAchievementUnlock";
        private static string UsernameKey => "username";

        public DateTimeOffset GetNextExecution(DateTimeOffset currentTime)
            => new ScheduledTimes(settings.ScheduledHours).GetNextExecution(currentTime);

        public async Task Execute()
        {
            logger.Info(
                MyOperation.ExecuteTimedLog,
                OperationStatus.Started,
                new LogInfo(MyLogInfoKey.Template, TemplateName));

            DateTimeOffset? latestLogTimestamp = await personalLogManagerService
                .GetLatestLogTimestamp(
                    TemplateName,
                    PlatformName,
                    settings.Username);
            IEnumerable<SteamAchievementUnlock> unlocks = await steamService
                .GetAchievementsUnlockedSince(latestLogTimestamp);
            SteamAchievementUnlock[] achievements = unlocks.ToArray();

            foreach (SteamAchievementUnlock achievement in achievements)
            {
                await SendAchievementUnlock(achievement);
            }

            logger.Info(
                MyOperation.ExecuteTimedLog,
                OperationStatus.Success,
                new LogInfo(MyLogInfoKey.Template, TemplateName),
                new LogInfo(MyLogInfoKey.AchievementCount, achievements.Length));
        }

        private async Task SendAchievementUnlock(SteamAchievementUnlock achievement)
        {
            Dictionary<string, string> data = new()
            {
                [AchievementNameKey] = achievement.AchievementName,
                [GameNameKey] = achievement.GameName,
                [PlatformKey] = PlatformName,
                [UsernameKey] = settings.Username
            };

            await personalLogManagerService.SendPersonalLogToManager(
                achievement.UnlockedAt,
                TemplateName,
                data);
        }
    }
}