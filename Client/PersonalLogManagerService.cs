using System;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

using NuciLog.Core;
using NuciAPI.Client;
using NuciAPI.Responses;

using PersonalDataLogger.Configuration;
using PersonalDataLogger.Logging;

namespace PersonalDataLogger.Client
{
    public class PersonalLogManagerService(
        PersonalLogManagerSettings settings,
        ILogger logger)
        : IPersonalLogManagerService
    {
        const string RomaniaTimeZoneId = "Europe/Bucharest";
        const string WindowsRomaniaTimeZoneId = "GTB Standard Time";

        readonly NuciApiClient apiClient = new(settings.BaseUrl);

        public Task SendPersonalLogToManager(
            DateTimeOffset timestamp,
            string template)
        {
            DateTimeOffset romaniaDateTime = ConvertToRomanianTime(timestamp);

            return SendPersonalLogToManager(
                romaniaDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                romaniaDateTime.ToString("HH:mm", CultureInfo.InvariantCulture),
                romaniaDateTime.ToString("zzz", CultureInfo.InvariantCulture),
                template);
        }

        public async Task SendPersonalLogToManager(
            string date,
            string time,
            string timeZone,
            string template)
        {
            IEnumerable<LogInfo> logInfos =
            [
                new(MyLogInfoKey.Date, date),
                new(MyLogInfoKey.Time, time),
                new(MyLogInfoKey.TimeZone, timeZone),
                new(MyLogInfoKey.Template, template)
            ];

            logger.Info(
                MyOperation.StoreLog,
                OperationStatus.Started,
                logInfos);

            NuciApiRequestAuthorisationInfo authorisationInfo = new()
            {
                BearerToken = settings.ApiKey,
                HmacSharedSecretKey = settings.HmacSharedSecretKey
            };

            NuciApiResponse response;

            try
            {
                response =
                    await apiClient.SendRequestAsync<StoreLogRequest, NuciApiSuccessResponse>(
                        HttpMethod.Post,
                        new StoreLogRequest()
                        {
                            Date = date,
                            Time = time,
                            TimeZone = timeZone,
                            Template = template
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
                NuciApiErrorResponse errorResponse = response as NuciApiErrorResponse;

                logger.Error(
                    MyOperation.StoreLog,
                    OperationStatus.Failure,
                    errorResponse.Message);

                throw new HttpRequestException(errorResponse.Message);
            }

            logger.Info(
                MyOperation.StoreLog,
                OperationStatus.Success,
                logInfos);
        }

        static DateTimeOffset ConvertToRomanianTime(DateTimeOffset dateTime)
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
