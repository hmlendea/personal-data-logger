using System;

namespace PersonalDataLogger.Client
{
    public sealed class SteamAchievementUnlock
    {
        public string AchievementName { get; set; }

        public string GameName { get; set; }

        public DateTimeOffset UnlockedAt { get; set; }
    }
}