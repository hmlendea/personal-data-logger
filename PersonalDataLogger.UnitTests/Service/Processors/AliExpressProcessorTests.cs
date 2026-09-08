using System;
using System.Collections.Generic;

using NUnit.Framework;
using Moq;

using PersonalDataLogger.Client;
using PersonalDataLogger.Configuration;
using PersonalDataLogger.Service.Models;
using PersonalDataLogger.Service.Processors;

namespace PersonalDataLogger.UnitTests.Service.Processors
{
    [TestFixture]
    public class AliExpressProcessorTests
    {
        private Mock<IPersonalLogManagerService> mockPersonalLogManagerService;
        private AliExpressSettings settings;
        private AliExpressProcessor processor;

        [SetUp]
        public void SetUp()
        {
            mockPersonalLogManagerService = new Mock<IPersonalLogManagerService>();
            settings = new AliExpressSettings
            {
                EmailAddress = "user@example.com"
            };

            processor = new AliExpressProcessor(mockPersonalLogManagerService.Object, settings);
        }

        [Test]
        public void GivenEmailWithVerificationCodeSubject_WhenProcessing_ThenPersonalLogManagerIsCalled()
        {
            AvailableEmail email = BuildAvailableEmail(
                subject: "Your AliExpress verification code");

            processor.ProcessEmail(email);

            mockPersonalLogManagerService.Verify(
                s => s.SendPersonalLogToManager(
                    email.Timestamp,
                    "AccountLogin",
                    It.IsAny<Dictionary<string, string>>()),
                Times.Once);
        }

        [Test]
        public void GivenEmailWithVerificationCodeSubject_WhenProcessing_ThenEmailAddressIsIncluded()
        {
            AvailableEmail email = BuildAvailableEmail(
                subject: "Your AliExpress verification code");

            processor.ProcessEmail(email);

            mockPersonalLogManagerService.Verify(
                s => s.SendPersonalLogToManager(
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<string>(),
                    It.Is<Dictionary<string, string>>(d =>
                        d["email_address"] == "user@example.com")),
                Times.Once);
        }

        [Test]
        public void GivenEmailWithVerificationCodeSubject_WhenProcessing_ThenPlatformIsAlwaysAliExpress()
        {
            AvailableEmail email = BuildAvailableEmail(
                subject: "Your AliExpress verification code");

            processor.ProcessEmail(email);

            mockPersonalLogManagerService.Verify(
                s => s.SendPersonalLogToManager(
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<string>(),
                    It.Is<Dictionary<string, string>>(d =>
                        d["platform"] == "AliExpress")),
                Times.Once);
        }

        [Test]
        public void GivenEmailWithoutVerificationCodeSubject_WhenProcessing_ThenPersonalLogManagerIsNotCalled()
        {
            AvailableEmail email = BuildAvailableEmail(
                subject: "Your order has been shipped");

            processor.ProcessEmail(email);

            mockPersonalLogManagerService.Verify(
                s => s.SendPersonalLogToManager(
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, string>>()),
                Times.Never);
        }

        [Test]
        public void GivenEmailWithPartialVerificationCodeSubject_WhenProcessing_ThenPersonalLogManagerIsNotCalled()
        {
            AvailableEmail email = BuildAvailableEmail(
                subject: "Your verification code");

            processor.ProcessEmail(email);

            mockPersonalLogManagerService.Verify(
                s => s.SendPersonalLogToManager(
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, string>>()),
                Times.Never);
        }

        [Test]
        public void GivenEmailWithDifferentPlatformVerificationCode_WhenProcessing_ThenPersonalLogManagerIsNotCalled()
        {
            AvailableEmail email = BuildAvailableEmail(
                subject: "Your Amazon verification code");

            processor.ProcessEmail(email);

            mockPersonalLogManagerService.Verify(
                s => s.SendPersonalLogToManager(
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, string>>()),
                Times.Never);
        }

        [Test]
        public void GivenEmailWithVerificationCodeInBody_WhenProcessing_ThenPersonalLogManagerIsNotCalled()
        {
            AvailableEmail email = BuildAvailableEmail(
                subject: "Important notification",
                body: "Your AliExpress verification code: 123456");

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
                subject: "your aliexpress verification code");

            processor.ProcessEmail(email);

            mockPersonalLogManagerService.Verify(
                s => s.SendPersonalLogToManager(
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, string>>()),
                Times.Once);
        }

        [Test]
        public void GivenEmailWithWhitespaceInSubject_WhenProcessing_ThenPersonalLogManagerIsCalled()
        {
            AvailableEmail email = BuildAvailableEmail(
                subject: "Your AliExpress verification code  ");

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
                subject: "URGENT: Your AliExpress verification code - DO NOT SHARE");

            processor.ProcessEmail(email);

            mockPersonalLogManagerService.Verify(
                s => s.SendPersonalLogToManager(
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, string>>()),
                Times.Once);
        }

        [Test]
        public void GivenEmptySubject_WhenProcessing_ThenPersonalLogManagerIsNotCalled()
        {
            AvailableEmail email = BuildAvailableEmail(subject: string.Empty);

            processor.ProcessEmail(email);

            mockPersonalLogManagerService.Verify(
                s => s.SendPersonalLogToManager(
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, string>>()),
                Times.Never);
        }

        [Test]
        public void GivenNullSubject_WhenProcessing_ThenExceptionIsThrown()
        {
            AvailableEmail email = new()
            {
                Uid = 1,
                Timestamp = DateTimeOffset.Now,
                Sender = "sender@example.com",
                Subject = null,
                Body = "Test body"
            };

            Assert.That(
                () => processor.ProcessEmail(email),
                Throws.InstanceOf<NullReferenceException>());
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
