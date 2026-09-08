using System;
using System.Collections.Generic;

using NUnit.Framework;
using Moq;

using PersonalDataLogger.Client;
using PersonalDataLogger.Service.Models;
using PersonalDataLogger.Service.Processors;

namespace PersonalDataLogger.Tests.Service.Processors
{
    [TestFixture]
    public class PayPalProcessorTests
    {
        private Mock<IPersonalLogManagerService> mockPersonalLogManagerService;
        private PayPalProcessor processor;

        [SetUp]
        public void SetUp()
        {
            mockPersonalLogManagerService = new Mock<IPersonalLogManagerService>();
            processor = new PayPalProcessor(mockPersonalLogManagerService.Object);
        }

        [Test]
        public void GivenEmailWithRomanianNewDeviceConnectionSubject_WhenProcessing_ThenPersonalLogManagerIsCalled()
        {
            AvailableEmail email = BuildAvailableEmail(
                subject: "Conectare de pe un dispozitiv nou",
                body: "contul tău PayPal, user@example.com:");

            processor.ProcessEmail(email);

            mockPersonalLogManagerService.Verify(
                s => s.SendPersonalLogToManager(
                    email.Timestamp,
                    "AccountLogin",
                    It.IsAny<Dictionary<string, string>>()),
                Times.Once);
        }

        [Test]
        public void GivenEmailWithRomanianNewDeviceConnectionAndValidEmail_WhenProcessing_ThenEmailIsExtracted()
        {
            AvailableEmail email = BuildAvailableEmail(
                subject: "Conectare de pe un dispozitiv nou",
                body: "contul tău PayPal, user@example.com:");

            processor.ProcessEmail(email);

            mockPersonalLogManagerService.Verify(
                s => s.SendPersonalLogToManager(
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<string>(),
                    It.Is<Dictionary<string, string>>(d =>
                        d["username"] == "user@example.com")),
                Times.Once);
        }

        [Test]
        public void GivenEmailWithRomanianNewDeviceConnectionAndNoEmail_WhenProcessing_ThenUsernameIsEmpty()
        {
            AvailableEmail email = BuildAvailableEmail(
                subject: "Conectare de pe un dispozitiv nou",
                body: "Some other content without email pattern");

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
        public void GivenEmailWithRomanianNewDeviceConnectionSubject_WhenProcessing_ThenPlatformIsAlwaysPayPal()
        {
            AvailableEmail email = BuildAvailableEmail(
                subject: "Conectare de pe un dispozitiv nou",
                body: "contul tău PayPal, test@example.com:");

            processor.ProcessEmail(email);

            mockPersonalLogManagerService.Verify(
                s => s.SendPersonalLogToManager(
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<string>(),
                    It.Is<Dictionary<string, string>>(d =>
                        d["platform"] == "PayPal")),
                Times.Once);
        }

        [Test]
        public void GivenEmailWithEnglishConnectionSubject_WhenProcessing_ThenPersonalLogManagerIsNotCalled()
        {
            AvailableEmail email = BuildAvailableEmail(
                subject: "New device connection",
                body: "contul tău PayPal, user@example.com:");

            processor.ProcessEmail(email);

            mockPersonalLogManagerService.Verify(
                s => s.SendPersonalLogToManager(
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, string>>()),
                Times.Never);
        }

        [Test]
        public void GivenEmailWithoutRomanianSubject_WhenProcessing_ThenPersonalLogManagerIsNotCalled()
        {
            AvailableEmail email = BuildAvailableEmail(
                subject: "Your transaction is complete",
                body: "contul tău PayPal, user@example.com:");

            processor.ProcessEmail(email);

            mockPersonalLogManagerService.Verify(
                s => s.SendPersonalLogToManager(
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, string>>()),
                Times.Never);
        }

        [Test]
        public void GivenEmailWithCaseVariationInSubject_WhenProcessing_ThenPersonalLogManagerIsCalled()
        {
            AvailableEmail email = BuildAvailableEmail(
                subject: "conectare de pe un dispozitiv nou",
                body: "contul tău PayPal, user@example.com:");

            processor.ProcessEmail(email);

            mockPersonalLogManagerService.Verify(
                s => s.SendPersonalLogToManager(
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, string>>()),
                Times.Once);
        }

        [Test]
        public void GivenEmailWithEmailContainingPlus_WhenProcessing_ThenEmailIsExtractedCorrectly()
        {
            AvailableEmail email = BuildAvailableEmail(
                subject: "Conectare de pe un dispozitiv nou",
                body: "contul tău PayPal, user+tag@example.com:");

            processor.ProcessEmail(email);

            mockPersonalLogManagerService.Verify(
                s => s.SendPersonalLogToManager(
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<string>(),
                    It.Is<Dictionary<string, string>>(d =>
                        d["username"] == "user+tag@example.com")),
                Times.Once);
        }

        [Test]
        public void GivenEmailWithMultipleEmailAddresses_WhenProcessing_ThenFirstEmailIsExtracted()
        {
            AvailableEmail email = BuildAvailableEmail(
                subject: "Conectare de pe un dispozitiv nou",
                body: "contul tău PayPal, first@example.com: and also second@example.com:");

            processor.ProcessEmail(email);

            mockPersonalLogManagerService.Verify(
                s => s.SendPersonalLogToManager(
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<string>(),
                    It.Is<Dictionary<string, string>>(d =>
                        d["username"] == "first@example.com")),
                Times.Once);
        }

        [Test]
        public void GivenEmailWithWhitespaceBeforeEmail_WhenProcessing_ThenEmailIsExtracted()
        {
            AvailableEmail email = BuildAvailableEmail(
                subject: "Conectare de pe un dispozitiv nou",
                body: "contul tău PayPal,   user@example.com:");

            processor.ProcessEmail(email);

            mockPersonalLogManagerService.Verify(
                s => s.SendPersonalLogToManager(
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<string>(),
                    It.Is<Dictionary<string, string>>(d =>
                        d["username"] == "user@example.com")),
                Times.Once);
        }

        [Test]
        public void GivenEmailWithoutColonAfterEmail_WhenProcessing_ThenEmailIsNotExtracted()
        {
            AvailableEmail email = BuildAvailableEmail(
                subject: "Conectare de pe un dispozitiv nou",
                body: "contul tău PayPal, user@example.com");

            processor.ProcessEmail(email);

            mockPersonalLogManagerService.Verify(
                s => s.SendPersonalLogToManager(
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<string>(),
                    It.Is<Dictionary<string, string>>(d =>
                        d["username"] == string.Empty)),
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
