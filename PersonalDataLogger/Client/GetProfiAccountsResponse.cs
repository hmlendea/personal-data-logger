using System.Collections.Generic;
using System.Text.Json.Serialization;

using NuciAPI.Responses;

namespace PersonalDataLogger.Client
{
    public sealed class GetProfiAccountsResponse : NuciApiSuccessResponse
    {
        [JsonPropertyName("accounts")]
        public IEnumerable<ProfiAccount> Accounts { get; set; } = [];
    }
}