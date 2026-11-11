using System.Text.Json.Serialization;

namespace PersonalDataLogger.Client
{
    public sealed class SteamGame
    {
        [JsonPropertyName("appid")]
        public int AppIdentifier { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("rtime_last_played")]
        public long LastPlayedTime { get; set; }
    }
}