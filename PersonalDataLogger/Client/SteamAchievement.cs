using System.Text.Json.Serialization;

namespace PersonalDataLogger.Client
{
    public sealed class SteamAchievement
    {
        [JsonPropertyName("apiname")]
        public string ApiName { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("achieved")]
        public int Achieved { get; set; }

        [JsonPropertyName("unlocktime")]
        public long UnlockTime { get; set; }
    }
}