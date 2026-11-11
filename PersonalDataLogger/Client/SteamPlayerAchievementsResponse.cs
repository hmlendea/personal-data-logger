using System.Text.Json.Serialization;

namespace PersonalDataLogger.Client
{
    public sealed class SteamPlayerAchievementsResponse
    {
        [JsonPropertyName("playerstats")]
        public SteamPlayerAchievements Response { get; set; }
    }
}