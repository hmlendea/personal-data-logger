using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PersonalDataLogger.Client
{
    public sealed class SteamPlayerAchievements
    {
        [JsonPropertyName("achievements")]
        public IEnumerable<SteamAchievement> Achievements { get; set; } = [];
    }
}