using System.Text.Json.Serialization;

namespace PersonalDataLogger.Client
{
    public sealed class ProfiAccount
    {
        [JsonPropertyName("balance")]
        public decimal Balance { get; set; }

        [JsonPropertyName("isEnabled")]
        public bool IsEnabled { get; set; }
    }
}