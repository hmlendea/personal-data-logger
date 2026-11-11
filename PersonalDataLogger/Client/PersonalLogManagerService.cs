using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using NuciAPI.Client;
using NuciAPI.Responses;

using NuciLog.Core;

using PersonalDataLogger.Configuration;
using PersonalDataLogger.Logging;

namespace PersonalDataLogger.Client
{
    public class PersonalLogManagerService(
        PersonalLogManagerSettings settings,
        ILogger logger,
        INuciApiClient apiClient) : IPersonalLogManagerService
    {
        private static string PersonalLogRejectionMessage =>
            "The Personal Log Manager rejected the personal log.";
        private static string InvalidLogsResponseMessage =>
            "The Personal Log Manager returned an invalid logs response.";
        private static string InvalidLogDateMessage =>
            "The Personal Log Manager returned a log with an invalid date.";
        private static string MissingLogsResponseMessage =>
            "The Personal Log Manager did not return a logs response.";
        private static string PersonalLogEndpoint => "/PersonalLog";
        private static string RomanianLocalisation => "ro-RO";
        private static string RomaniaTimeZoneCode => "RO";
        private static string LogDateFormat => "yyyy-MM-dd";
        private static string LogTimeFormat => "HH:mm";
        private static int LatestLogSearchCount => 10000;
        private static readonly Regex LogTimestampPattern = new(
            @"(?<date>\d{4}-\d{2}-\d{2})\s*:?\s*(?<time>\d{2}:\d{2})?",
            RegexOptions.Compiled);

        public PersonalLogManagerService(
            PersonalLogManagerSettings settings,
            ILogger logger)
            : this(settings, logger, new NuciApiClient(settings.BaseUrl))
        {
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
            DateTimeOffset romaniaDateTime = RomaniaTimeZone.ConvertToRomanianTime(timestamp);

            return SendPersonalLogToManager(
                romaniaDateTime.ToString(LogDateFormat, CultureInfo.InvariantCulture),
                romaniaDateTime.ToString(LogTimeFormat, CultureInfo.InvariantCulture),
                RomaniaTimeZoneCode,
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

            NuciApiRequestAuthorisationInfo authorisationInfo = GetAuthorisationInfo();

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
                        PersonalLogEndpoint);

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

        public async Task<DateTimeOffset?> GetLatestLogTimestamp(
            string template,
            string platform,
            string username)
        {
            IEnumerable<LogInfo> logInfos =
            [
                new(MyLogInfoKey.Template, template),
                new(MyLogInfoKey.Platform, platform),
                new(MyLogInfoKey.Username, username)
            ];

            logger.Info(
                MyOperation.GetLatestLogDate,
                OperationStatus.Started,
                logInfos);

            NuciApiResponse response;

            try
            {
                response =
                    await apiClient.SendRequestAsync<GetPersonalLogsRequest, GetPersonalLogsResponse>(
                        HttpMethod.Get,
                        new GetPersonalLogsRequest
                        {
                            Template = template,
                            Localisation = RomanianLocalisation,
                            Count = LatestLogSearchCount,
                            Platform = platform,
                            Username = username
                        },
                        GetAuthorisationInfo(),
                        PersonalLogEndpoint);
            }
            catch (Exception exception)
            {
                string errorMessage = "Error while retrieving the latest personal log date.";

                logger.Error(
                    MyOperation.GetLatestLogDate,
                    OperationStatus.Failure,
                    errorMessage,
                    exception,
                    logInfos);

                throw new HttpRequestException(errorMessage, exception);
            }

            DateTimeOffset? latestLogTimestamp = GetLatestLogTimestamp(response);

            if (latestLogTimestamp is not null)
            {
                logInfos = logInfos.Append(new(MyLogInfoKey.Date, latestLogTimestamp));
            }

            logger.Info(
                MyOperation.GetLatestLogDate,
                OperationStatus.Success,
                logInfos);

            return latestLogTimestamp;
        }

        private NuciApiRequestAuthorisationInfo GetAuthorisationInfo()
            => new()
            {
                BearerToken = settings.ApiKey,
                ClientId = settings.ClientId,
                HmacSharedSecretKey = settings.HmacSharedSecretKey
            };

        private static DateTimeOffset? GetLatestLogTimestamp(NuciApiResponse response)
        {
            if (response is null)
            {
                throw new HttpRequestException(MissingLogsResponseMessage);
            }

            if (!response.IsSuccessful)
            {
                throw new HttpRequestException(response.Message);
            }

            if (response is not GetPersonalLogsResponse logsResponse)
            {
                throw new HttpRequestException(InvalidLogsResponseMessage);
            }

            string latestLog = logsResponse.Logs?.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(latestLog))
            {
                return null;
            }

            Match match = LogTimestampPattern.Match(latestLog);

            if (!match.Success ||
                !DateOnly.TryParseExact(
                    match.Groups["date"].Value,
                    LogDateFormat,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateOnly logDate))
            {
                throw new HttpRequestException(InvalidLogDateMessage);
            }

            TimeOnly? logTime = null;

            if (match.Groups["time"].Success &&
                TimeOnly.TryParseExact(
                    match.Groups["time"].Value,
                    LogTimeFormat,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out TimeOnly parsedTime))
            {
                logTime = parsedTime;
            }

            return RomaniaTimeZone.GetRomanianTimestamp(logDate, logTime);
        }
    }
}
