using NuciAPI.Requests;

namespace PersonalDataLogger.Client
{
    public sealed class GetProfiAccountsRequest : NuciApiRequest
    {
        public static GetProfiAccountsRequest Instance => new();
    }
}