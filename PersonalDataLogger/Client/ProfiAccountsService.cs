using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using NuciAPI.Client;
using NuciAPI.Responses;

using NuciLog.Core;

using PersonalDataLogger.Configuration;
using PersonalDataLogger.Logging;

namespace PersonalDataLogger.Client
{
    public sealed class ProfiAccountsService(
        ProfiBotServerSettings settings,
        INuciApiClient apiClient) : IProfiAccountsService
    {
        private readonly ILogger logger;

        private static string BalanceCalculationFailureMessage =>
            "The enabled Profi account balance calculation failed.";
        private static string GetAccountsFailureMessage =>
            "The Profi Bot Server accounts request failed.";
        private static string InvalidAccountsResponseMessage =>
            "The Profi Bot Server returned an invalid accounts response.";
        private static string MissingAccountsMessage =>
            "The Profi Bot Server accounts response did not contain an account collection.";
        private static string MissingResponseMessage =>
            "The Profi Bot Server did not return an accounts response.";
        private static string UsernameToken => "{username}";

        public ProfiAccountsService(ProfiBotServerSettings settings)
            : this(settings, new NuciApiClient(settings.BaseUrl))
        {
        }

        public ProfiAccountsService(
            ProfiBotServerSettings settings,
            INuciApiClient apiClient,
            ILogger logger)
            : this(settings, apiClient)
        {
            this.logger = logger;
        }

        public async Task<decimal> GetEnabledAccountsBalance()
        {
            ProfiAccount[] accounts = await GetAccounts();

            return CalculateEnabledAccountsBalance(accounts);
        }

        private decimal CalculateEnabledAccountsBalance(ProfiAccount[] accounts)
        {
            IEnumerable<LogInfo> logInfos =
            [
                new(MyLogInfoKey.AccountCount, accounts.Length)
            ];
            LogInformation(
                MyOperation.CalculateProfiAccountsBalance,
                OperationStatus.Started,
                logInfos);

            try
            {
                ProfiAccount[] enabledAccounts = accounts
                    .Where(account => account.IsEnabled)
                    .ToArray();
                decimal balance = enabledAccounts.Sum(account => account.Balance);
                logInfos = logInfos
                    .Append(new(MyLogInfoKey.EnabledAccountCount, enabledAccounts.Length))
                    .Append(new(MyLogInfoKey.Amount, balance));
                LogInformation(
                    MyOperation.CalculateProfiAccountsBalance,
                    OperationStatus.Success,
                    logInfos);

                return balance;
            }
            catch (Exception exception)
            {
                LogFailure(
                    MyOperation.CalculateProfiAccountsBalance,
                    BalanceCalculationFailureMessage,
                    exception,
                    logInfos);

                throw;
            }
        }

        private async Task<ProfiAccount[]> GetAccounts()
        {
            IEnumerable<LogInfo> requestLogInfos =
            [
                new(MyLogInfoKey.HttpMethod, HttpMethod.Get.Method)
            ];
            LogInformation(
                MyOperation.GetProfiAccounts,
                OperationStatus.Started,
                requestLogInfos);

            ProfiAccount[] accounts;

            try
            {
                NuciApiResponse response = await SendGetAccountsRequest();

                if (response is not null)
                {
                    requestLogInfos = requestLogInfos.Append(
                        new(MyLogInfoKey.ResponseCode, response.Code));
                }

                accounts = GetAccountsFromResponse(response);
            }
            catch (Exception exception)
            {
                LogFailure(
                    MyOperation.GetProfiAccounts,
                    GetAccountsFailureMessage,
                    exception,
                    requestLogInfos);

                throw;
            }

            requestLogInfos = requestLogInfos.Append(
                new(MyLogInfoKey.AccountCount, accounts.Length));
            LogInformation(
                MyOperation.GetProfiAccounts,
                OperationStatus.Success,
                requestLogInfos);

            return accounts;
        }

        private static ProfiAccount[] GetAccountsFromResponse(NuciApiResponse response)
        {
            if (response is null)
            {
                throw new HttpRequestException(MissingResponseMessage);
            }

            if (!response.IsSuccessful)
            {
                throw new HttpRequestException(response.Message);
            }

            if (response is not GetProfiAccountsResponse accountsResponse)
            {
                throw new HttpRequestException(InvalidAccountsResponseMessage);
            }

            if (accountsResponse.Accounts is null)
            {
                throw new HttpRequestException(MissingAccountsMessage);
            }

            return accountsResponse.Accounts.ToArray();
        }

        private Task<NuciApiResponse> SendGetAccountsRequest()
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

            return apiClient.SendRequestAsync<GetProfiAccountsRequest, GetProfiAccountsResponse>(
                HttpMethod.Get,
                GetProfiAccountsRequest.Instance,
                authorisationInfo,
                endpoint);
        }

        private void LogFailure(
            Operation operation,
            string message,
            Exception exception,
            IEnumerable<LogInfo> logInfos)
        {
            if (logger is null)
            {
                return;
            }

            logger.Error(
                operation,
                OperationStatus.Failure,
                message,
                exception,
                logInfos);
        }

        private void LogInformation(
            Operation operation,
            OperationStatus status,
            IEnumerable<LogInfo> logInfos)
        {
            if (logger is null)
            {
                return;
            }

            logger.Info(
                operation,
                status,
                logInfos);
        }
    }
}