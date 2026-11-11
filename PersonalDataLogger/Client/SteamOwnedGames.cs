using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PersonalDataLogger.Client
{
    public sealed class SteamOwnedGames
    {
        [JsonPropertyName("games")]
        public IEnumerable<SteamGame> Games { get; set; } = [];
    }
}