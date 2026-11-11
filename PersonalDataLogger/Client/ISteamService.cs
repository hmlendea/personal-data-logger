using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PersonalDataLogger.Client
{
    public interface ISteamService
    {
        Task<IEnumerable<SteamAchievementUnlock>> GetAchievementsUnlockedSince(
            DateTimeOffset? cutoffTimestamp);
    }
}