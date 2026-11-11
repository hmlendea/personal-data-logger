using System.Text.Json.Serialization;

namespace PersonalDataLogger.Client
{
    public sealed class SteamVanityUrlResponse
    {
        [JsonPropertyName("response")]
        public SteamVanityUrl Response { get; set; }
    }
}