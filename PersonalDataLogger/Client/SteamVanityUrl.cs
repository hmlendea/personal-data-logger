using System.Text.Json.Serialization;

namespace PersonalDataLogger.Client
{
    public sealed class SteamVanityUrl
    {
        [JsonPropertyName("success")]
        public int Success { get; set; }

        [JsonPropertyName("steamid")]
        public string SteamIdentifier { get; set; }
    }
}