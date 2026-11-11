using System.Text.Json.Serialization;

namespace PersonalDataLogger.Client
{
    public sealed class SteamOwnedGamesResponse
    {
        [JsonPropertyName("response")]
        public SteamOwnedGames Response { get; set; }
    }
}