using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

using NUnit.Framework;
using Moq;

using NuciLog.Core;

using PersonalDataLogger.Client;
using PersonalDataLogger.Configuration;
using PersonalDataLogger.Service;

namespace PersonalDataLogger.Tests.Service
{
    [TestFixture]
    public class ProfiBalanceTimedLogTests
    {
        private Mock<IProfiAccountsService> mockProfiAccountsService;
        private Mock<IPersonalLogManagerService> mockPersonalLogManagerService;
        private Mock<ILogger> mockLogger;
        private ProfiBotServerSettings settings;
        private ProfiBalanceTimedLog timedLog;

        [SetUp]
        public void SetUp()
        {
            mockProfiAccountsService = new Mock<IProfiAccountsService>();
            mockPersonalLogManagerService = new Mock<IPersonalLogManagerService>();
            mockLogger = new Mock<ILogger>();

            settings = new ProfiBotServerSettings
            {
                AccountName = "TestAccount"
            };

            timedLog = new ProfiBalanceTimedLog(
                mockProfiAccountsService.Object,
                mockPersonalLogManagerService.Object,
                settings,
                mockLogger.Object);
        }

        [Test]
        public void GivenCurrentTimeBeforeScheduledTime_WhenGettingNextExecution_ThenReturnsScheduledTimeToday()
        {
            // Test at 6:00 AM, scheduled time is 6:30 AM
            DateTime currentTime = new(2026, 9, 8, 6, 0, 0);
            DateTimeOffset current = new(currentTime, TimeZoneInfo.Local.GetUtcOffset(currentTime));

            DateTimeOffset nextExecution = timedLog.GetNextExecution(current);

            Assert.That(
                nextExecution.LocalDateTime.Date,
                Is.EqualTo(currentTime.Date));
            Assert.That(
                nextExecution.LocalDateTime.Hour,
                Is.EqualTo(6));
            Assert.That(
                nextExecution.LocalDateTime.Minute,
                Is.EqualTo(30));
        }

        [Test]
        public void GivenCurrentTimeAfterScheduledTime_WhenGettingNextExecution_ThenReturnsScheduledTimeTomorrow()
        {
            // Test at 7:00 AM, scheduled time is 6:30 AM
            DateTime currentTime = new(2026, 9, 8, 7, 0, 0);
            DateTimeOffset current = new(currentTime, TimeZoneInfo.Local.GetUtcOffset(currentTime));

            DateTimeOffset nextExecution = timedLog.GetNextExecution(current);

            Assert.That(
                nextExecution.LocalDateTime.Date,
                Is.EqualTo(currentTime.Date.AddDays(1)));
            Assert.That(
                nextExecution.LocalDateTime.Hour,
                Is.EqualTo(6));
            Assert.That(
                nextExecution.LocalDateTime.Minute,
                Is.EqualTo(30));
        }

        [Test]
        public void GivenCurrentTimeExactlyAtScheduledTime_WhenGettingNextExecution_ThenReturnsScheduledTimeTomorrow()
        {
            // Test at exactly 6:30 AM
            DateTime currentTime = new(2026, 9, 8, 6, 30, 0);
            DateTimeOffset current = new(currentTime, TimeZoneInfo.Local.GetUtcOffset(currentTime));

            DateTimeOffset nextExecution = timedLog.GetNextExecution(current);

            Assert.That(
                nextExecution.LocalDateTime.Date,
                Is.EqualTo(currentTime.Date.AddDays(1)));
        }

        [Test]
        public void GivenCurrentTimeSlightlyBeforeScheduledTime_WhenGettingNextExecution_ThenReturnsScheduledTimeToday()
        {
            // Test at 6:29:59 AM
            DateTime currentTime = new(2026, 9, 8, 6, 29, 59);
            DateTimeOffset current = new(currentTime, TimeZoneInfo.Local.GetUtcOffset(currentTime));

            DateTimeOffset nextExecution = timedLog.GetNextExecution(current);

            Assert.That(
                nextExecution.LocalDateTime.Date,
                Is.EqualTo(currentTime.Date));
            Assert.That(
                nextExecution.LocalDateTime.Hour,
                Is.EqualTo(6));
        }

        [Test]
        public void GivenCurrentTimeSlightlyAfterScheduledTime_WhenGettingNextExecution_ThenReturnsScheduledTimeTomorrow()
        {
            // Test at 6:30:01 AM
            DateTime currentTime = new(2026, 9, 8, 6, 30, 1);
            DateTimeOffset current = new(currentTime, TimeZoneInfo.Local.GetUtcOffset(currentTime));

            DateTimeOffset nextExecution = timedLog.GetNextExecution(current);

            Assert.That(
                nextExecution.LocalDateTime.Date,
                Is.EqualTo(currentTime.Date.AddDays(1)));
        }

        [Test]
        public void GivenCurrentTimeAtMidnight_WhenGettingNextExecution_ThenReturnsScheduledTimeToday()
        {
            // Test at 00:00 (midnight)
            DateTime currentTime = new(2026, 9, 8, 0, 0, 0);
            DateTimeOffset current = new(currentTime, TimeZoneInfo.Local.GetUtcOffset(currentTime));

            DateTimeOffset nextExecution = timedLog.GetNextExecution(current);

            Assert.That(
                nextExecution.LocalDateTime.Date,
                Is.EqualTo(currentTime.Date));
            Assert.That(
                nextExecution.LocalDateTime.Hour,
                Is.EqualTo(6));
            Assert.That(
                nextExecution.LocalDateTime.Minute,
                Is.EqualTo(30));
        }

        [Test]
        public void GivenCurrentTimeAtElevenPM_WhenGettingNextExecution_ThenReturnsScheduledTimeTomorrow()
        {
            // Test at 23:00 (11 PM)
            DateTime currentTime = new(2026, 9, 8, 23, 0, 0);
            DateTimeOffset current = new(currentTime, TimeZoneInfo.Local.GetUtcOffset(currentTime));

            DateTimeOffset nextExecution = timedLog.GetNextExecution(current);

            Assert.That(
                nextExecution.LocalDateTime.Date,
                Is.EqualTo(currentTime.Date.AddDays(1)));
        }

        [Test]
        public void GivenNextExecutionCalculation_WhenCheckingTimeZoneOffset_ThenOffsetIsPreserved()
        {
            DateTime currentTime = new(2026, 9, 8, 6, 0, 0);
            DateTimeOffset current = new(currentTime, TimeZoneInfo.Local.GetUtcOffset(currentTime));

            DateTimeOffset nextExecution = timedLog.GetNextExecution(current);

            TimeSpan expectedOffset = TimeZoneInfo.Local.GetUtcOffset(nextExecution.LocalDateTime);
            Assert.That(
                nextExecution.Offset,
                Is.EqualTo(expectedOffset));
        }

        [Test]
        public async Task GivenValidBalanceFromService_WhenExecuting_ThenPersonalLogManagerIsCalledWithCorrectData()
        {
            mockProfiAccountsService
                .Setup(s => s.GetEnabledAccountsBalance())
                .ReturnsAsync(100.50m);

            await timedLog.Execute();

            mockPersonalLogManagerService.Verify(
                s => s.SendPersonalLogToManager(
                    It.IsAny<DateTimeOffset>(),
                    "BotsTotalBalanceMeasurement",
                    It.Is<Dictionary<string, string>>(d =>
                        d.ContainsKey("platform") &&
                        d.ContainsKey("account") &&
                        d.ContainsKey("amount") &&
                        d.ContainsKey("currency"))),
                Times.Once);
        }

        [Test]
        public async Task GivenZeroBalance_WhenExecuting_ThenPersonalLogManagerIsCalledWithZeroAmount()
        {
            mockProfiAccountsService
                .Setup(s => s.GetEnabledAccountsBalance())
                .ReturnsAsync(0m);

            await timedLog.Execute();

            mockPersonalLogManagerService.Verify(
                s => s.SendPersonalLogToManager(
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<string>(),
                    It.Is<Dictionary<string, string>>(d =>
                        d["amount"] == "0")),
                Times.Once);
        }

        [Test]
        public async Task GivenNegativeBalance_WhenExecuting_ThenNegativeAmountIsForwarded()
        {
            mockProfiAccountsService
                .Setup(s => s.GetEnabledAccountsBalance())
                .ReturnsAsync(-50.25m);

            await timedLog.Execute();

            mockPersonalLogManagerService.Verify(
                s => s.SendPersonalLogToManager(
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<string>(),
                    It.Is<Dictionary<string, string>>(d =>
                        d["amount"] == "-50.25")),
                Times.Once);
        }

        [Test]
        public async Task GivenDecimalBalance_WhenExecuting_ThenInvariantCultureIsUsedForFormatting()
        {
            mockProfiAccountsService
                .Setup(s => s.GetEnabledAccountsBalance())
                .ReturnsAsync(123.456m);

            await timedLog.Execute();

            mockPersonalLogManagerService.Verify(
                s => s.SendPersonalLogToManager(
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<string>(),
                    It.Is<Dictionary<string, string>>(d =>
                        d["amount"] == "123.456")),
                Times.Once);
        }

        [Test]
        public async Task GivenSuccessfulExecution_WhenExecuting_ThenLoggerLogsStartAndSuccess()
        {
            mockProfiAccountsService
                .Setup(s => s.GetEnabledAccountsBalance())
                .ReturnsAsync(50m);

            await timedLog.Execute();

            mockLogger.Verify(
                l => l.Info(
                    It.IsAny<IOperationBase>(),
                    OperationStatus.Started,
                    It.IsAny<IEnumerable<LogInfo>>()),
                Times.Once);

            mockLogger.Verify(
                l => l.Info(
                    It.IsAny<IOperationBase>(),
                    OperationStatus.Success,
                    It.IsAny<IEnumerable<LogInfo>>()),
                Times.Once);
        }

        [Test]
        public void GivenProfiAccountsServiceThrows_WhenExecuting_ThenExceptionPropagates()
        {
            mockProfiAccountsService
                .Setup(s => s.GetEnabledAccountsBalance())
                .ThrowsAsync(new HttpRequestException("API Error"));

            Assert.That(
                async () => await timedLog.Execute(),
                Throws.InstanceOf<HttpRequestException>());
        }

        [Test]
        public void GivenPersonalLogManagerServiceThrows_WhenExecuting_ThenExceptionPropagates()
        {
            mockProfiAccountsService
                .Setup(s => s.GetEnabledAccountsBalance())
                .ReturnsAsync(50m);

            mockPersonalLogManagerService
                .Setup(s => s.SendPersonalLogToManager(
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, string>>()))
                .ThrowsAsync(new HttpRequestException("Logging failed"));

            Assert.That(
                async () => await timedLog.Execute(),
                Throws.InstanceOf<HttpRequestException>());
        }

        [Test]
        public async Task GivenSettingsWithAccountName_WhenExecuting_ThenAccountNameIsIncluded()
        {
            settings.AccountName = "MyProfiAccount";
            mockProfiAccountsService
                .Setup(s => s.GetEnabledAccountsBalance())
                .ReturnsAsync(100m);

            await timedLog.Execute();

            mockPersonalLogManagerService.Verify(
                s => s.SendPersonalLogToManager(
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<string>(),
                    It.Is<Dictionary<string, string>>(d =>
                        d["account"] == "MyProfiAccount")),
                Times.Once);
        }

        [Test]
        public async Task GivenExecution_WhenSendingToPersonalLogManager_ThenCurrencyIsAlwaysRON()
        {
            mockProfiAccountsService
                .Setup(s => s.GetEnabledAccountsBalance())
                .ReturnsAsync(100m);

            await timedLog.Execute();

            mockPersonalLogManagerService.Verify(
                s => s.SendPersonalLogToManager(
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<string>(),
                    It.Is<Dictionary<string, string>>(d =>
                        d["currency"] == "RON")),
                Times.Once);
        }

        [Test]
        public async Task GivenExecution_WhenSendingToPersonalLogManager_ThenPlatformIsAlwaysProfiBot()
        {
            mockProfiAccountsService
                .Setup(s => s.GetEnabledAccountsBalance())
                .ReturnsAsync(100m);

            await timedLog.Execute();

            mockPersonalLogManagerService.Verify(
                s => s.SendPersonalLogToManager(
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<string>(),
                    It.Is<Dictionary<string, string>>(d =>
                        d["platform"] == "Profi Bot Server")),
                Times.Once);
        }
    }
}
