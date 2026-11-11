using System.Collections.Generic;
using System.Text.Json.Serialization;

using NuciAPI.Responses;

namespace PersonalDataLogger.Client
{
    public sealed class GetPersonalLogsResponse : NuciApiSuccessResponse
    {
        [JsonPropertyName("logs")]
        public IEnumerable<string> Logs { get; set; } = [];
    }
}