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
    public class OpsGenieEmailProcessorTests
    {
        private Mock<IPersonalLogManagerService> mockPersonalLogManagerService;
        private PersonalSettings settings;
        private OpsGenieEmailProcessor processor;

        [SetUp]
        public void SetUp()
        {
            mockPersonalLogManagerService = new Mock<IPersonalLogManagerService>();
            settings = new PersonalSettings
            {
                EmployerName = "ACME Corp"
            };

            processor = new OpsGenieEmailProcessor(mockPersonalLogManagerService.Object, settings);
        }

        [Test]
        public void GivenEmailWithOnCallShiftBeginningSubject_WhenProcessing_ThenPersonalLogManagerIsCalledWithBeginningTemplate()
        {
            AvailableEmail email = BuildAvailableEmail(
                subject: "Your on-call rotation is starting now");

            processor.ProcessEmail(email);

            mockPersonalLogManagerService.Verify(
                s => s.SendPersonalLogToManager(
                    email.Timestamp,
                    "WorkOnCallShiftBeginning",
                    It.IsAny<Dictionary<string, string>>()),
                Times.Once);
        }

        [Test]
        public void GivenEmailWithOnCallShiftBeginningSubject_WhenProcessing_ThenEmployerNameIsIncluded()
        {
            AvailableEmail email = BuildAvailableEmail(
                subject: "Your on-call rotation is starting now");

            processor.ProcessEmail(email);

            mockPersonalLogManagerService.Verify(
                s => s.SendPersonalLogToManager(
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<string>(),
                    It.Is<Dictionary<string, string>>(d =>
                        d["employer_name"] == "ACME Corp")),
                Times.Once);
        }

        [Test]
        public void GivenEmailWithOnCallShiftEndingSubject_WhenProcessing_ThenPersonalLogManagerIsCalledWithEndingTemplate()
        {
            AvailableEmail email = BuildAvailableEmail(
                subject: "Your on-call rotation is ending now");

            processor.ProcessEmail(email);

            mockPersonalLogManagerService.Verify(
                s => s.SendPersonalLogToManager(
                    email.Timestamp,
                    "WorkOnCallShiftEnding"),
                Times.Once);
        }

        [Test]
        public void GivenEmailWithOnCallShiftEndingSubject_WhenProcessing_ThenNoDataIncluded()
        {
            AvailableEmail email = BuildAvailableEmail(
                subject: "Your on-call rotation is ending now");

            processor.ProcessEmail(email);

            mockPersonalLogManagerService.Verify(
                s => s.SendPersonalLogToManager(
                    It.IsAny<DateTimeOffset>(),
                    "WorkOnCallShiftEnding"),
                Times.Once);
        }

        [Test]
        public void GivenEmailWithOnCallRotationButNoStartingOrEnding_WhenProcessing_ThenPersonalLogManagerIsNotCalled()
        {
            AvailableEmail email = BuildAvailableEmail(
                subject: "Your on-call rotation has changed");

            processor.ProcessEmail(email);

            mockPersonalLogManagerService.Verify(
                s => s.SendPersonalLogToManager(
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, string>>()),
                Times.Never);
        }

        [Test]
        public void GivenEmailWithoutOnCallRotationSubject_WhenProcessing_ThenPersonalLogManagerIsNotCalled()
        {
            AvailableEmail email = BuildAvailableEmail(
                subject: "Alert notification");

            processor.ProcessEmail(email);

            mockPersonalLogManagerService.Verify(
                s => s.SendPersonalLogToManager(
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, string>>()),
                Times.Never);
        }

        [Test]
        public void GivenEmailWithOnCallRotationStartingButCaseVariation_WhenProcessing_ThenPersonalLogManagerIsCalled()
        {
            AvailableEmail email = BuildAvailableEmail(
                subject: "your on-call rotation is starting now");

            processor.ProcessEmail(email);

            mockPersonalLogManagerService.Verify(
                s => s.SendPersonalLogToManager(
                    It.IsAny<DateTimeOffset>(),
                    "WorkOnCallShiftBeginning",
                    It.IsAny<Dictionary<string, string>>()),
                Times.Once);
        }

        [Test]
        public void GivenEmailWithOnCallRotationEndingButCaseVariation_WhenProcessing_ThenPersonalLogManagerIsCalled()
        {
            AvailableEmail email = BuildAvailableEmail(
                subject: "YOUR ON-CALL ROTATION IS ENDING NOW");

            processor.ProcessEmail(email);

            mockPersonalLogManagerService.Verify(
                s => s.SendPersonalLogToManager(
                    It.IsAny<DateTimeOffset>(),
                    "WorkOnCallShiftEnding"),
                Times.Once);
        }

        [Test]
        public void GivenEmailWithOnCallRotationStartingWithExtraText_WhenProcessing_ThenPersonalLogManagerIsNotCalled()
        {
            AvailableEmail email = BuildAvailableEmail(
                subject: "Your on-call rotation is starting now - verify details");

            processor.ProcessEmail(email);

            mockPersonalLogManagerService.Verify(
                s => s.SendPersonalLogToManager(
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, string>>()),
                Times.Never);
        }

        [Test]
        public void GivenEmailWithOnCallRotationEndingWithExtraText_WhenProcessing_ThenPersonalLogManagerIsNotCalled()
        {
            AvailableEmail email = BuildAvailableEmail(
                subject: "Your on-call rotation is ending now - thank you");

            processor.ProcessEmail(email);

            mockPersonalLogManagerService.Verify(
                s => s.SendPersonalLogToManager(
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, string>>()),
                Times.Never);
        }

        [Test]
        public void GivenEmailStartsWithOnCallRotationButMissingIsStartingNow_WhenProcessing_ThenPersonalLogManagerIsNotCalled()
        {
            AvailableEmail email = BuildAvailableEmail(
                subject: "Your on-call rotation begins today");

            processor.ProcessEmail(email);

            mockPersonalLogManagerService.Verify(
                s => s.SendPersonalLogToManager(
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, string>>()),
                Times.Never);
        }

        [Test]
        public void GivenSettingsWithDifferentEmployerName_WhenProcessingBeginning_ThenCorrectEmployerNameIsIncluded()
        {
            settings.EmployerName = "XYZ Inc";
            AvailableEmail email = BuildAvailableEmail(
                subject: "Your on-call rotation is starting now");

            processor.ProcessEmail(email);

            mockPersonalLogManagerService.Verify(
                s => s.SendPersonalLogToManager(
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<string>(),
                    It.Is<Dictionary<string, string>>(d =>
                        d["employer_name"] == "XYZ Inc")),
                Times.Once);
        }

        [Test]
        public void GivenEmailWithOnCallRotationEnding_WhenProcessing_ThenNoEmployerNameIsForwarded()
        {
            AvailableEmail email = BuildAvailableEmail(
                subject: "Your on-call rotation is ending now");

            processor.ProcessEmail(email);

            mockPersonalLogManagerService.Verify(
                s => s.SendPersonalLogToManager(
                    It.IsAny<DateTimeOffset>(),
                    "WorkOnCallShiftEnding"),
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
