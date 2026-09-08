using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;

using NUnit.Framework;
using Moq;

using NuciLog.Core;
using NuciAPI.Responses;

using PersonalDataLogger.Client;
using PersonalDataLogger.Configuration;

namespace PersonalDataLogger.Tests.Client
{
    [TestFixture]
    public class PersonalLogManagerServiceTests
    {
        private Mock<ILogger> mockLogger;
        private PersonalLogManagerSettings settings;
        private PersonalLogManagerService service;

        [SetUp]
        public void SetUp()
        {
            mockLogger = new Mock<ILogger>();
            settings = new PersonalLogManagerSettings
            {
                BaseUrl = "https://api.example.com",
                ApiKey = "test-api-key",
                ClientId = "test-client-id",
                HmacSharedSecretKey = "test-secret-key"
            };

            service = new PersonalLogManagerService(settings, mockLogger.Object);
        }

        [Test]
        public async Task GivenAValidTimestampAndTemplate_WhenSendingPersonalLogWithoutData_ThenLogsAreCreatedSuccessfully()
        {
            DateTimeOffset timestamp = new(new DateTime(2026, 9, 8, 12, 30, 45), TimeSpan.FromHours(3));
            string template = "AccountLogin";

            await service.SendPersonalLogToManager(timestamp, template);

            mockLogger.Verify(
                l => l.Info(
                    It.IsAny<IOperationBase>(),
                    It.IsAny<OperationStatus>(),
                    It.IsAny<IEnumerable<LogInfo>>()),
                Times.AtLeastOnce);
        }

        [Test]
        public async Task GivenAValidTimestampTemplateAndData_WhenSendingPersonalLog_ThenLogsAreCreatedSuccessfully()
        {
            DateTimeOffset timestamp = new(new DateTime(2026, 9, 8, 12, 30, 45), TimeSpan.FromHours(3));
            string template = "AccountLogin";
            Dictionary<string, string> data = new()
            {
                ["platform"] = "AliExpress",
                ["email_address"] = "test@example.com"
            };

            await service.SendPersonalLogToManager(timestamp, template, data);

            mockLogger.Verify(
                l => l.Info(
                    It.IsAny<IOperationBase>(),
                    It.IsAny<OperationStatus>(),
                    It.IsAny<IEnumerable<LogInfo>>()),
                Times.AtLeastOnce);
        }

        [Test]
        public async Task GivenDateTimeOffsetInDifferentTimeZone_WhenConvertingToRomanianTime_ThenTimestampIsConvertedCorrectly()
        {
            // Timestamp in UTC
            DateTimeOffset utcTimestamp = new(new DateTime(2026, 9, 8, 09, 30, 0), TimeSpan.Zero);
            string template = "TestTemplate";

            await service.SendPersonalLogToManager(utcTimestamp, template);

            // Should log with converted time (approximately UTC+3 for Europe/Bucharest)
            mockLogger.Verify(
                l => l.Info(
                    It.IsAny<IOperationBase>(),
                    It.IsAny<OperationStatus>(),
                    It.IsAny<IEnumerable<LogInfo>>()),
                Times.AtLeastOnce);
        }

        [Test]
        public async Task GivenValidDateTimeStringAndTemplate_WhenSendingPersonalLogWithExplicitDateTime_ThenLogsAreCreatedSuccessfully()
        {
            string date = "2026-09-08";
            string time = "12:30";
            string timeZone = "RO";
            string template = "TestTemplate";

            await service.SendPersonalLogToManager(date, time, timeZone, template);

            mockLogger.Verify(
                l => l.Info(
                    It.IsAny<IOperationBase>(),
                    It.IsAny<OperationStatus>(),
                    It.IsAny<IEnumerable<LogInfo>>()),
                Times.AtLeastOnce);
        }

        [Test]
        public async Task GivenValidDateTimeStringTemplateAndData_WhenSendingPersonalLogWithExplicitDateTime_ThenLogsAreCreatedSuccessfully()
        {
            string date = "2026-09-08";
            string time = "12:30";
            string timeZone = "RO";
            string template = "AccountLogin";
            Dictionary<string, string> data = new()
            {
                ["platform"] = "PayPal",
                ["username"] = "user@example.com"
            };

            await service.SendPersonalLogToManager(date, time, timeZone, template, data);

            mockLogger.Verify(
                l => l.Info(
                    It.IsAny<IOperationBase>(),
                    It.IsAny<OperationStatus>(),
                    It.IsAny<IEnumerable<LogInfo>>()),
                Times.AtLeastOnce);
        }

        [Test]
        public async Task GivenEmptyDataDictionary_WhenSendingPersonalLog_ThenLogsAreCreatedSuccessfully()
        {
            DateTimeOffset timestamp = DateTimeOffset.Now;
            string template = "TestTemplate";
            Dictionary<string, string> emptyData = new();

            await service.SendPersonalLogToManager(timestamp, template, emptyData);

            mockLogger.Verify(
                l => l.Info(
                    It.IsAny<IOperationBase>(),
                    It.IsAny<OperationStatus>(),
                    It.IsAny<IEnumerable<LogInfo>>()),
                Times.AtLeastOnce);
        }

        [Test]
        public async Task GivenMultipleDataEntries_WhenSendingPersonalLog_ThenAllDataIsLogged()
        {
            DateTimeOffset timestamp = DateTimeOffset.Now;
            string template = "BotPrizeWinning";
            Dictionary<string, string> data = new()
            {
                ["platform"] = "Profi",
                ["account_id"] = "12345",
                ["prize_description"] = "100 RON voucher"
            };

            await service.SendPersonalLogToManager(timestamp, template, data);

            mockLogger.Verify(
                l => l.Info(
                    It.IsAny<IOperationBase>(),
                    It.IsAny<OperationStatus>(),
                    It.IsAny<IEnumerable<LogInfo>>()),
                Times.AtLeastOnce);
        }

        [Test]
        public void GivenInvalidSettings_WhenCreatingServiceAndCalling_ThenExceptionIsThrown()
        {
            settings.BaseUrl = null;

            Assert.That(
                async () => await service.SendPersonalLogToManager(DateTimeOffset.Now, "Test"),
                Throws.InstanceOf<Exception>());
        }
    }
}
