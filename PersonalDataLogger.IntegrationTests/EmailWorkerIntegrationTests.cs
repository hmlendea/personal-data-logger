using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using Moq;
using Moq.Protected;

using NUnit.Framework;

using NuciLog.Core;

using PersonalDataLogger.Client;
using PersonalDataLogger.Configuration;
using PersonalDataLogger.Service;
using PersonalDataLogger.Service.Models;
using PersonalDataLogger.Service.Processors;

namespace PersonalDataLogger.IntegrationTests
{
    [TestFixture]
    [NonParallelizable]
    public class EmailWorkerIntegrationTests
    {
        private static string CheckpointFilePath => Path.Combine(
            AppContext.BaseDirectory,
            "imap-checkpoint.json");

        private Mock<IEmailProcessor> mockEmailProcessor = null!;
        private Mock<IPersonalLogManagerService> mockPersonalLogManagerService = null!;
        private Mock<EmailWorker> mockEmailWorker = null!;

        [SetUp]
        public void SetUp()
        {
            File.Delete(CheckpointFilePath);
            mockEmailProcessor = new Mock<IEmailProcessor>();
            mockPersonalLogManagerService = new Mock<IPersonalLogManagerService>();
            mockPersonalLogManagerService
                .Setup(service => service.SendPersonalLogToManager(
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            mockPersonalLogManagerService
                .Setup(service => service.SendPersonalLogToManager(
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, string>>()))
                .Returns(Task.CompletedTask);

            mockEmailWorker = new Mock<EmailWorker>(
                new AliExpressProcessor(
                    mockPersonalLogManagerService.Object,
                    new AliExpressSettings { EmailAddress = "solaire@astora.com" }),
                new GandiProcessor(mockPersonalLogManagerService.Object),
                new OpsGenieEmailProcessor(
                    mockPersonalLogManagerService.Object,
                    new PersonalSettings { EmployerName = "NuciSoft" }),
                new PayPalProcessor(mockPersonalLogManagerService.Object),
                new ProfiProcessor(mockPersonalLogManagerService.Object),
                mockEmailProcessor.Object,
                new ImapSettings { MaxEmailAge = 3600 },
                Mock.Of<ILogger>())
            {
                CallBase = true
            };
            mockEmailWorker.Protected().Setup("WaitForNextPoll");
        }

        [TearDown]
        public void TearDown()
            => File.Delete(CheckpointFilePath);

        [Test]
        public void GivenCurrentEmailsFromAllSupportedSenders_WhenWatchingEmails_ThenStoresTheirLogs()
        {
            DateTimeOffset timestamp = DateTimeOffset.UtcNow;
            InvalidOperationException terminalFailure = new("The mailbox is unavailable.");
            AvailableEmailBatch currentEmails = new()
            {
                UidValidity = 1024,
                Emails =
                [
                    BuildEmail(4, timestamp, "security@aliexpress.com", "Your AliExpress verification code", string.Empty),
                    BuildEmail(8, timestamp, "security@gandi.net", "Connection on a new device", "username solaire. IP address: 203.0.113.4"),
                    BuildEmail(16, timestamp, "notifications@opsgenie.com", "Your on-call rotation is starting now", string.Empty),
                    BuildEmail(32, timestamp, "notifications@opsgenie.com", "Your on-call rotation is ending now", string.Empty),
                    BuildEmail(42, timestamp, "notify@profi_bot_server.com", "Profi Prize Won", "Account: prize (613)\nItem: 100 RON voucher"),
                    BuildEmail(48, timestamp, "security@gandi_net", "Connection on a new device", "username solaire. IP address: 203.0.113.8"),
                    BuildEmail(64, timestamp, "security@paypal.com", "Conectare de pe un dispozitiv nou", "contul tău PayPal, solaire@astora.com:")
                ]
            };
            mockEmailProcessor
                .SetupSequence(processor => processor.GetAvailableEmails(It.IsAny<uint>()))
                .Returns(new AvailableEmailBatch { UidValidity = 1024 })
                .Returns(currentEmails)
                .Throws(terminalFailure);

            Assert.That(
                () => mockEmailWorker.Object.WatchEmails(),
                Throws.Exception.SameAs(terminalFailure));

            VerifyDataLog("AccountLogin", "AliExpress", 1);
            VerifyDataLog("AccountLogin", "Gandi", 2);
            VerifyDataLog("WorkOnCallShiftBeginning", null, 1);
            VerifyDataLog("BotPrizeWinning", "Profi", 1);
            VerifyDataLog("AccountLogin", "PayPal", 1);
            mockPersonalLogManagerService.Verify(
                service => service.SendPersonalLogToManager(timestamp, "WorkOnCallShiftEnding"),
                Times.Once);
            mockEmailProcessor.Verify(processor => processor.LogIn(), Times.Once);
            mockEmailProcessor.Verify(processor => processor.LogOut(), Times.Once);
        }

        [Test]
        public void GivenStaleAndUnrecognizedEmails_WhenWatchingEmails_ThenDoesNotStoreLogs()
        {
            DateTimeOffset staleTimestamp = DateTimeOffset.UtcNow.AddHours(-2);
            InvalidOperationException terminalFailure = new("The mailbox is unavailable.");
            mockEmailProcessor
                .SetupSequence(processor => processor.GetAvailableEmails(It.IsAny<uint>()))
                .Returns(new AvailableEmailBatch
                {
                    Emails =
                    [
                        BuildEmail(4, staleTimestamp, "security@aliexpress.com", "Your AliExpress verification code", string.Empty),
                        BuildEmail(8, DateTimeOffset.UtcNow, "newsletter@nucisoft.ro", "Weekly update", string.Empty)
                    ]
                })
                .Throws(terminalFailure);

            Assert.That(
                () => mockEmailWorker.Object.WatchEmails(),
                Throws.Exception.SameAs(terminalFailure));
            mockPersonalLogManagerService.Verify(
                service => service.SendPersonalLogToManager(
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<string>()),
                Times.Never);
            mockPersonalLogManagerService.Verify(
                service => service.SendPersonalLogToManager(
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, string>>()),
                Times.Never);
        }

        private void VerifyDataLog(string template, string? platform, int expectedCallCount)
            => mockPersonalLogManagerService.Verify(
                service => service.SendPersonalLogToManager(
                    It.IsAny<DateTimeOffset>(),
                    template,
                    It.Is<Dictionary<string, string>>(data => HasPlatform(data, platform))),
                Times.Exactly(expectedCallCount));

        private static bool HasPlatform(Dictionary<string, string> data, string? platform)
        {
            if (platform is null)
            {
                return true;
            }

            return data.ContainsKey("platform") &&
                string.Equals(data["platform"], platform, StringComparison.Ordinal);
        }

        private static AvailableEmail BuildEmail(
            uint uid,
            DateTimeOffset timestamp,
            string sender,
            string subject,
            string body)
            => new()
            {
                Uid = uid,
                Timestamp = timestamp,
                Sender = sender,
                Subject = subject,
                Body = body
            };
    }
}