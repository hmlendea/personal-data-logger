using System.Text.Json.Serialization;

using NuciAPI.Requests;

namespace PersonalDataLogger.Client
{
    public sealed class GetPersonalLogsRequest : NuciApiRequest
    {
        [JsonPropertyName("template")]
        public string Template { get; set; }

        [JsonPropertyName("localisation")]
        public string Localisation { get; set; }

        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("data.platform")]
        public string Platform { get; set; }

        [JsonPropertyName("data.username")]
        public string Username { get; set; }
    }
}