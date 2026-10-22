using System;
using System.Collections.Generic;

using NUnit.Framework;
using Moq;

using PersonalDataLogger.Client;
using PersonalDataLogger.Service.Models;
using PersonalDataLogger.Service.Processors;

namespace PersonalDataLogger.UnitTests.Service.Processors
{
    [TestFixture]
    public class DiscordProcessorTests
    {
        private Mock<IPersonalLogManagerService> mockPersonalLogManagerService = null!;
        private DiscordProcessor processor = null!;

        [SetUp]
        public void SetUp()
        {
            mockPersonalLogManagerService = new Mock<IPersonalLogManagerService>();
            processor = new DiscordProcessor(mockPersonalLogManagerService.Object);
        }

        [Test]
        public void GivenEmailWithVerifyDiscordLoginSubject_WhenProcessing_ThenPersonalLogManagerIsCalled()
        {
            AvailableEmail email = BuildAvailableEmail(
                subject: "Verify Discord Login from New Location",
                body: "IP Address: 192.168.1.1\nLocation: Cluj-Napoca, Cluj County, Romania");

            processor.ProcessEmail(email);

            mockPersonalLogManagerService.Verify(
                s => s.SendPersonalLogToManager(
                    email.Timestamp,
                    "AccountLogin",
                    It.IsAny<Dictionary<string, string>>()),
                Times.Once);
        }

        [Test]
        public void GivenEmailWithVerifyDiscordLoginSubjectAndValidIP_WhenProcessing_ThenIPAddressIsExtracted()
        {
            AvailableEmail email = BuildAvailableEmail(
                subject: "Verify Discord Login from New Location",
                body: "IP Address: 192.168.1.1\nLocation: Cluj-Napoca, Cluj County, Romania");

            processor.ProcessEmail(email);

            mockPersonalLogManagerService.Verify(
                s => s.SendPersonalLogToManager(
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<string>(),
                    It.Is<Dictionary<string, string>>(d =>
                        d["ip_address"] == "192.168.1.1")),
                Times.Once);
        }

        [Test]
        public void GivenEmailWithVerifyDiscordLoginSubjectAndValidLocation_WhenProcessing_ThenLocationIsExtracted()
        {
            AvailableEmail email = BuildAvailableEmail(
                subject: "Verify Discord Login from New Location",
                body: "IP Address: 192.168.1.1\nLocation: Cluj-Napoca, Cluj County, Romania");

            processor.ProcessEmail(email);

            mockPersonalLogManagerService.Verify(
                s => s.SendPersonalLogToManager(
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<string>(),
                    It.Is<Dictionary<string, string>>(d =>
                        d["location"] == "Cluj-Napoca, Cluj County, Romania")),
                Times.Once);
        }

        [Test]
        public void GivenEmailWithVerifyDiscordLoginSubjectAndNoIP_WhenProcessing_ThenIPAddressIsEmpty()
        {
            AvailableEmail email = BuildAvailableEmail(
                subject: "Verify Discord Login from New Location",
                body: "Location: Cluj-Napoca, Cluj County, Romania");

            processor.ProcessEmail(email);

            mockPersonalLogManagerService.Verify(
                s => s.SendPersonalLogToManager(
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<string>(),
                    It.Is<Dictionary<string, string>>(d =>
                        d["ip_address"] == string.Empty)),
                Times.Once);
        }

        [Test]
        public void GivenEmailWithVerifyDiscordLoginSubjectAndNoLocation_WhenProcessing_ThenLocationIsEmpty()
        {
            AvailableEmail email = BuildAvailableEmail(
                subject: "Verify Discord Login from New Location",
                body: "IP Address: 192.168.1.1");

            processor.ProcessEmail(email);

            mockPersonalLogManagerService.Verify(
                s => s.SendPersonalLogToManager(
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<string>(),
                    It.Is<Dictionary<string, string>>(d =>
                        d["location"] == string.Empty)),
                Times.Once);
        }

        [Test]
        public void GivenEmailWithVerifyDiscordLoginSubject_WhenProcessing_ThenPlatformIsAlwaysDiscord()
        {
            AvailableEmail email = BuildAvailableEmail(
                subject: "Verify Discord Login from New Location",
                body: "IP Address: 10.0.0.1\nLocation: Somewhere");

            processor.ProcessEmail(email);

            mockPersonalLogManagerService.Verify(
                s => s.SendPersonalLogToManager(
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<string>(),
                    It.Is<Dictionary<string, string>>(d =>
                        d["platform"] == "Discord")),
                Times.Once);
        }

        [Test]
        public void GivenEmailWithoutVerifyDiscordLoginSubject_WhenProcessing_ThenPersonalLogManagerIsNotCalled()
        {
            AvailableEmail email = BuildAvailableEmail(
                subject: "Your Discord account was updated",
                body: "IP Address: 192.168.1.1\nLocation: Cluj-Napoca, Cluj County, Romania");

            processor.ProcessEmail(email);

            mockPersonalLogManagerService.Verify(
                s => s.SendPersonalLogToManager(
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, string>>()),
                Times.Never);
        }

        [Test]
        public void GivenEmailWithVerifyDiscordLoginSubjectCaseInsensitive_WhenProcessing_ThenPersonalLogManagerIsCalled()
        {
            AvailableEmail email = BuildAvailableEmail(
                subject: "VERIFY DISCORD LOGIN FROM NEW LOCATION",
                body: "IP Address: 192.168.1.1\nLocation: Cluj-Napoca, Cluj County, Romania");

            processor.ProcessEmail(email);

            mockPersonalLogManagerService.Verify(
                s => s.SendPersonalLogToManager(
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, string>>()),
                Times.Once);
        }

        [Test]
        public void GivenEmailWithIPv6Address_WhenProcessing_ThenIPv6IsNotMatched()
        {
            AvailableEmail email = BuildAvailableEmail(
                subject: "Verify Discord Login from New Location",
                body: "IP Address: 2001:0db8:85a3:0000:0000:8a2e:0370:7334\nLocation: Somewhere");

            processor.ProcessEmail(email);

            mockPersonalLogManagerService.Verify(
                s => s.SendPersonalLogToManager(
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<string>(),
                    It.Is<Dictionary<string, string>>(d =>
                        d["ip_address"] == string.Empty)),
                Times.Once);
        }

        private static AvailableEmail BuildAvailableEmail(
            string subject,
            string body)
        {
            return new AvailableEmail
            {
                Uid = 1,
                Subject = subject,
                Body = body,
                Sender = "noreply@discord.com",
                Timestamp = DateTimeOffset.UtcNow
            };
        }
    }
}