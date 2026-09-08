using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using NuciAPI.Client;
using NuciAPI.Responses;

using PersonalDataLogger.Configuration;

namespace PersonalDataLogger.Client
{
    public sealed class ProfiAccountsService : IProfiAccountsService
    {
        private static string UsernameToken => "{username}";

        private readonly ProfiBotServerSettings settings;
        private readonly INuciApiClient apiClient;

        public ProfiAccountsService(ProfiBotServerSettings settings)
            : this(settings, new NuciApiClient(settings.BaseUrl))
        {
        }

        public ProfiAccountsService(
            ProfiBotServerSettings settings,
            INuciApiClient apiClient)
        {
            this.settings = settings;
            this.apiClient = apiClient;
        }

        public async Task<decimal> GetEnabledAccountsBalance()
        {
            NuciApiRequestAuthorisationInfo authorisationInfo = new()
            {
                BearerToken = settings.UserApiKey,
                ClientId = settings.ClientId,
                HmacSharedSecretKey = settings.HmacSharedSecretKey
            };
            string endpoint = settings.AccountsEndpoint.Replace(
                UsernameToken,
                Uri.EscapeDataString(settings.Username),
                StringComparison.Ordinal);
            NuciApiResponse response = await apiClient.SendRequestAsync<GetProfiAccountsRequest, GetProfiAccountsResponse>(
                HttpMethod.Get,
                GetProfiAccountsRequest.Instance,
                authorisationInfo,
                endpoint);

            if (!response.IsSuccessful)
            {
                throw new HttpRequestException(response.Message);
            }

            if (response is not GetProfiAccountsResponse accountsResponse)
            {
                throw new HttpRequestException("The Profi Bot Server returned an invalid accounts response.");
            }

            return accountsResponse.Accounts
                .Where(account => account.IsEnabled)
                .Sum(account => account.Balance);
        }
    }
}