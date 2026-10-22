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
    public class GoogleProcessorTests
    {
        private Mock<IPersonalLogManagerService> mockPersonalLogManagerService = null!;
        private GoogleProcessor processor = null!;

        [SetUp]
        public void SetUp()
        {
            mockPersonalLogManagerService = new Mock<IPersonalLogManagerService>();
            processor = new GoogleProcessor(mockPersonalLogManagerService.Object);
        }

        [Test]
        public void GivenEmailWithSecurityAlertSubject_WhenProcessing_ThenPersonalLogManagerIsCalled()
        {
            AvailableEmail email = BuildAvailableEmail(
                subject: "Security alert",
                body: "hori873hacks@gmail.com");

            processor.ProcessEmail(email);

            mockPersonalLogManagerService.Verify(
                s => s.SendPersonalLogToManager(
                    email.Timestamp,
                    "AccountLogin",
                    It.IsAny<Dictionary<string, string>>()),
                Times.Once);
        }

        [Test]
        public void GivenEmailWithSecurityAlertSubjectAndValidEmail_WhenProcessing_ThenUsernameIsExtracted()
        {
            AvailableEmail email = BuildAvailableEmail(
                subject: "Security alert",
                body: "hori873hacks@gmail.com");

            processor.ProcessEmail(email);

            mockPersonalLogManagerService.Verify(
                s => s.SendPersonalLogToManager(
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<string>(),
                    It.Is<Dictionary<string, string>>(d =>
                        d["username"] == "hori873hacks")),
                Times.Once);
        }

        [Test]
        public void GivenEmailWithSecurityAlertSubjectAndEmailWithDots_WhenProcessing_ThenUsernameIsExtracted()
        {
            AvailableEmail email = BuildAvailableEmail(
                subject: "Security alert",
                body: "john.doe@gmail.com");

            processor.ProcessEmail(email);

            mockPersonalLogManagerService.Verify(
                s => s.SendPersonalLogToManager(
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<string>(),
                    It.Is<Dictionary<string, string>>(d =>
                        d["username"] == "john.doe")),
                Times.Once);
        }

        [Test]
        public void GivenEmailWithSecurityAlertSubjectAndEmailWithHyphen_WhenProcessing_ThenUsernameIsExtracted()
        {
            AvailableEmail email = BuildAvailableEmail(
                subject: "Security alert",
                body: "john-doe@gmail.com");

            processor.ProcessEmail(email);

            mockPersonalLogManagerService.Verify(
                s => s.SendPersonalLogToManager(
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<string>(),
                    It.Is<Dictionary<string, string>>(d =>
                        d["username"] == "john-doe")),
                Times.Once);
        }

        [Test]
        public void GivenEmailWithSecurityAlertSubjectAndNoEmail_WhenProcessing_ThenUsernameIsEmpty()
        {
            AvailableEmail email = BuildAvailableEmail(
                subject: "Security alert",
                body: "Some text without email");

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
        public void GivenEmailWithSecurityAlertSubject_WhenProcessing_ThenPlatformIsAlwaysGoogle()
        {
            AvailableEmail email = BuildAvailableEmail(
                subject: "Security alert",
                body: "testuser@gmail.com");

            processor.ProcessEmail(email);

            mockPersonalLogManagerService.Verify(
                s => s.SendPersonalLogToManager(
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<string>(),
                    It.Is<Dictionary<string, string>>(d =>
                        d["platform"] == "Google")),
                Times.Once);
        }

        [Test]
        public void GivenEmailWithoutSecurityAlertSubject_WhenProcessing_ThenPersonalLogManagerIsNotCalled()
        {
            AvailableEmail email = BuildAvailableEmail(
                subject: "Your Google account was updated",
                body: "testuser@gmail.com");

            processor.ProcessEmail(email);

            mockPersonalLogManagerService.Verify(
                s => s.SendPersonalLogToManager(
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, string>>()),
                Times.Never);
        }

        [Test]
        public void GivenEmailWithSecurityAlertSubjectCaseInsensitive_WhenProcessing_ThenPersonalLogManagerIsCalled()
        {
            AvailableEmail email = BuildAvailableEmail(
                subject: "SECURITY ALERT",
                body: "testuser@gmail.com");

            processor.ProcessEmail(email);

            mockPersonalLogManagerService.Verify(
                s => s.SendPersonalLogToManager(
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, string>>()),
                Times.Once);
        }

        [Test]
        public void GivenEmailWithRomanianSecurityAlertSubject_WhenProcessing_ThenPersonalLogManagerIsCalled()
        {
            AvailableEmail email = BuildAvailableEmail(
                subject: "Alertă de securitate",
                body: "melendea.horatiu@gmail.com");

            processor.ProcessEmail(email);

            mockPersonalLogManagerService.Verify(
                s => s.SendPersonalLogToManager(
                    email.Timestamp,
                    "AccountLogin",
                    It.Is<Dictionary<string, string>>(d =>
                        d["platform"] == "Google" &&
                        d["username"] == "melendea.horatiu")),
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
                Sender = "no-reply@accounts.google.com",
                Timestamp = DateTimeOffset.UtcNow
            };
        }
    }
}