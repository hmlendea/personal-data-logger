using System;
using System.Collections.Generic;
using System.Globalization;
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
    public class PersonalLogManagerService : IPersonalLogManagerService
    {
        private readonly PersonalLogManagerSettings settings;
        private readonly ILogger logger;
        private readonly INuciApiClient apiClient;

        private static string PersonalLogRejectionMessage =>
            "The Personal Log Manager rejected the personal log.";
        private static string RomaniaTimeZoneId => "Europe/Bucharest";
        private static string WindowsRomaniaTimeZoneId => "GTB Standard Time";

        public PersonalLogManagerService(
            PersonalLogManagerSettings settings,
            ILogger logger)
            : this(settings, logger, new NuciApiClient(settings.BaseUrl))
        {
        }

        public PersonalLogManagerService(
            PersonalLogManagerSettings settings,
            ILogger logger,
            INuciApiClient apiClient)
        {
            this.settings = settings;
            this.logger = logger;
            this.apiClient = apiClient;
        }

        public Task SendPersonalLogToManager(
            DateTimeOffset timestamp,
            string template)
            => SendPersonalLogToManager(timestamp, template, []);

        public Task SendPersonalLogToManager(
            DateTimeOffset timestamp,
            string template,
            Dictionary<string, string> data)
        {
            DateTimeOffset romaniaDateTime = ConvertToRomanianTime(timestamp);

            return SendPersonalLogToManager(
                romaniaDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                romaniaDateTime.ToString("HH:mm", CultureInfo.InvariantCulture),
                "RO",
                template,
                data);
        }

        public async Task SendPersonalLogToManager(
            string date,
            string time,
            string timeZone,
            string template)
            => await SendPersonalLogToManager(date, time, timeZone, template, []);

        public async Task SendPersonalLogToManager(
            string date,
            string time,
            string timeZone,
            string template,
            Dictionary<string, string> data)
        {
            IEnumerable<LogInfo> logInfos =
            [
                new(MyLogInfoKey.Date, date),
                new(MyLogInfoKey.Time, time),
                new(MyLogInfoKey.TimeZone, timeZone),
                new(MyLogInfoKey.Template, template),
                new(MyLogInfoKey.Data, data)
            ];

            logger.Info(
                MyOperation.StoreLog,
                OperationStatus.Started,
                logInfos);

            NuciApiRequestAuthorisationInfo authorisationInfo = new()
            {
                BearerToken = settings.ApiKey,
                ClientId = settings.ClientId,
                HmacSharedSecretKey = settings.HmacSharedSecretKey
            };

            NuciApiResponse response;

            try
            {
                response =
                    await apiClient.SendRequestAsync<StoreLogRequest, NuciApiSuccessResponse>(
                        HttpMethod.Post,
                        new StoreLogRequest
                        {
                            Date = date,
                            Time = time,
                            TimeZone = timeZone,
                            Template = template,
                            Data = data
                        },
                        authorisationInfo,
                        "/PersonalLog");

                logInfos = logInfos
                    .Append(new(MyLogInfoKey.ResponseCode, response.Code));
            }
            catch (Exception exception)
            {
                string errorMessage = "Error while sending the personal log to the Personal Log Manager.";

                logger.Error(
                    MyOperation.StoreLog,
                    OperationStatus.Failure,
                    errorMessage,
                    exception,
                    logInfos);

                throw new HttpRequestException(errorMessage, exception);
            }

            if (!response.IsSuccessful)
            {
                HttpRequestException exception = new(response.Message);

                logger.Error(
                    MyOperation.StoreLog,
                    OperationStatus.Failure,
                    PersonalLogRejectionMessage,
                    exception,
                    logInfos);

                throw exception;
            }

            logger.Info(
                MyOperation.StoreLog,
                OperationStatus.Success,
                logInfos);
        }

        private static DateTimeOffset ConvertToRomanianTime(DateTimeOffset dateTime)
        {
            TimeZoneInfo romanianDateTime;

            try
            {
                romanianDateTime = TimeZoneInfo.FindSystemTimeZoneById(RomaniaTimeZoneId);
            }
            catch (TimeZoneNotFoundException)
            {
                romanianDateTime = TimeZoneInfo.FindSystemTimeZoneById(WindowsRomaniaTimeZoneId);
            }

            return TimeZoneInfo.ConvertTime(dateTime, romanianDateTime);
        }
    }
}
