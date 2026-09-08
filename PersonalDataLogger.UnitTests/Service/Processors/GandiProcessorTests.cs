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
    public class GandiProcessorTests
    {
        private Mock<IPersonalLogManagerService> mockPersonalLogManagerService;
        private GandiProcessor processor;

        [SetUp]
        public void SetUp()
        {
            mockPersonalLogManagerService = new Mock<IPersonalLogManagerService>();
            processor = new GandiProcessor(mockPersonalLogManagerService.Object);
        }

        [Test]
        public void GivenEmailWithNewDeviceConnectionSubject_WhenProcessing_ThenPersonalLogManagerIsCalled()
        {
            AvailableEmail email = BuildAvailableEmail(
                subject: "connection on a new device",
                body: "username testuser. IP address: 192.168.1.1");

            processor.ProcessEmail(email);

            mockPersonalLogManagerService.Verify(
                s => s.SendPersonalLogToManager(
                    email.Timestamp,
                    "AccountLogin",
                    It.IsAny<Dictionary<string, string>>()),
                Times.Once);
        }

        [Test]
        public void GivenEmailWithNewDeviceConnectionAndValidUsername_WhenProcessing_ThenUsernameIsExtracted()
        {
            AvailableEmail email = BuildAvailableEmail(
                subject: "connection on a new device",
                body: "username testuser. IP address: 192.168.1.1");

            processor.ProcessEmail(email);

            mockPersonalLogManagerService.Verify(
                s => s.SendPersonalLogToManager(
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<string>(),
                    It.Is<Dictionary<string, string>>(d =>
                        d["username"] == "testuser")),
                Times.Once);
        }

        [Test]
        public void GivenEmailWithNewDeviceConnectionAndValidIPAddress_WhenProcessing_ThenIPAddressIsExtracted()
        {
            AvailableEmail email = BuildAvailableEmail(
                subject: "connection on a new device",
                body: "username testuser. IP address: 192.168.1.1");

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
        public void GivenEmailWithNewDeviceConnectionAndNoUsername_WhenProcessing_ThenUsernameIsEmpty()
        {
            AvailableEmail email = BuildAvailableEmail(
                subject: "connection on a new device",
                body: "IP address: 192.168.1.1");

            processor.ProcessEmail(email);

            mockPersonalLogManagerService.Verify(
                s => s.SendPersonalLogToManager(
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<string>(),
                    It.Is<Dictionary<string, string>>(d =>
                        d["username"] == string.Empty)),
                Times.Once);
        }

        [Test]
        public void GivenEmailWithNewDeviceConnectionAndNoIPAddress_WhenProcessing_ThenIPAddressIsEmpty()
        {
            AvailableEmail email = BuildAvailableEmail(
                subject: "connection on a new device",
                body: "username testuser");

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
        public void GivenEmailWithNewDeviceConnectionSubject_WhenProcessing_ThenPlatformIsAlwaysGandi()
        {
            AvailableEmail email = BuildAvailableEmail(
                subject: "connection on a new device",
                body: "username user. IP address: 10.0.0.1");

            processor.ProcessEmail(email);

            mockPersonalLogManagerService.Verify(
                s => s.SendPersonalLogToManager(
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<string>(),
                    It.Is<Dictionary<string, string>>(d =>
                        d["platform"] == "Gandi")),
                Times.Once);
        }

        [Test]
        public void GivenEmailWithoutNewDeviceConnectionSubject_WhenProcessing_ThenPersonalLogManagerIsNotCalled()
        {
            AvailableEmail email = BuildAvailableEmail(
                subject: "Your domain has been renewed",
                body: "username testuser. IP address: 192.168.1.1");

            processor.ProcessEmail(email);

            mockPersonalLogManagerService.Verify(
                s => s.SendPersonalLogToManager(
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, string>>()),
                Times.Never);
        }

        [Test]
        public void GivenEmailWithIPv6Address_WhenProcessing_ThenIPv6IsNotMatched()
        {
            AvailableEmail email = BuildAvailableEmail(
                subject: "connection on a new device",
                body: "username testuser. IP address: 2001:0db8:85a3:0000:0000:8a2e:0370:7334");

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
        public void GivenEmailWithInvalidIPFormat_WhenProcessing_ThenIPAddressIsEmpty()
        {
            AvailableEmail email = BuildAvailableEmail(
                subject: "connection on a new device",
                body: "username testuser. IP address: invalid-ip");

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
        public void GivenEmailWithMultipleIPAddresses_WhenProcessing_ThenFirstIPIsExtracted()
        {
            AvailableEmail email = BuildAvailableEmail(
                subject: "connection on a new device",
                body: "username testuser. IP address: 192.168.1.1 and also 10.0.0.1");

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
        public void GivenEmailWithCaseVariationInSubject_WhenProcessing_ThenPersonalLogManagerIsCalled()
        {
            AvailableEmail email = BuildAvailableEmail(
                subject: "Connection on a new device",
                body: "username user. IP address: 192.168.1.1");

            processor.ProcessEmail(email);

            mockPersonalLogManagerService.Verify(
                s => s.SendPersonalLogToManager(
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, string>>()),
                Times.Once);
        }

        [Test]
        public void GivenEmailWithExtraTextInSubject_WhenProcessing_ThenPersonalLogManagerIsCalled()
        {
            AvailableEmail email = BuildAvailableEmail(
                subject: "ALERT: connection on a new device - verify immediately",
                body: "username user. IP address: 192.168.1.1");

            processor.ProcessEmail(email);

            mockPersonalLogManagerService.Verify(
                s => s.SendPersonalLogToManager(
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, string>>()),
                Times.Once);
        }

        [Test]
        public void GivenEmailWithUsernameSurroundedByWhitespace_WhenProcessing_ThenUsernameIsExtractedCorrectly()
        {
            AvailableEmail email = BuildAvailableEmail(
                subject: "connection on a new device",
                body: "username   testuser   . IP address: 192.168.1.1");

            processor.ProcessEmail(email);

            mockPersonalLogManagerService.Verify(
                s => s.SendPersonalLogToManager(
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<string>(),
                    It.Is<Dictionary<string, string>>(d =>
                        d["username"] == "testuser")),
                Times.Once);
        }

        [Test]
        public void GivenEmailWithIPAddressNoLeadingZeros_WhenProcessing_ThenIPAddressIsExtracted()
        {
            AvailableEmail email = BuildAvailableEmail(
                subject: "connection on a new device",
                body: "username user. IP address: 1.2.3.4");

            processor.ProcessEmail(email);

            mockPersonalLogManagerService.Verify(
                s => s.SendPersonalLogToManager(
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<string>(),
                    It.Is<Dictionary<string, string>>(d =>
                        d["ip_address"] == "1.2.3.4")),
                Times.Once);
        }

        private static AvailableEmail BuildAvailableEmail(
            string subject = "Test Subject",
            string body = "Test Body",
            uint uid = 1)
            => new()
            {
                Uid = uid,
                Timestamp = DateTimeOffset.Now,
                Sender = "sender@example.com",
                Subject = subject,
                Body = body
            };
    }
}
