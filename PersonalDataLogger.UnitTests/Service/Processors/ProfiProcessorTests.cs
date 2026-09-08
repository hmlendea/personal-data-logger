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
    public class ProfiProcessorTests
    {
        private Mock<IPersonalLogManagerService> mockPersonalLogManagerService;
        private ProfiProcessor processor;

        [SetUp]
        public void SetUp()
        {
            mockPersonalLogManagerService = new Mock<IPersonalLogManagerService>();
            processor = new ProfiProcessor(mockPersonalLogManagerService.Object);
        }

        [Test]
        public void GivenEmailWithProfiPrizeWonSubject_WhenProcessing_ThenPersonalLogManagerIsCalled()
        {
            AvailableEmail email = BuildAvailableEmail(
                subject: "Profi Prize Won",
                body: "Account: User Account (12345)\nItem: 100 RON Voucher");

            processor.ProcessEmail(email);

            mockPersonalLogManagerService.Verify(
                s => s.SendPersonalLogToManager(
                    email.Timestamp,
                    "BotPrizeWinning",
                    It.IsAny<Dictionary<string, string>>()),
                Times.Once);
        }

        [Test]
        public void GivenEmailWithProfiPrizeWonAndValidAccountId_WhenProcessing_ThenAccountIdIsExtracted()
        {
            AvailableEmail email = BuildAvailableEmail(
                subject: "Profi Prize Won",
                body: "Account: User Account (12345)\nItem: 100 RON Voucher");

            processor.ProcessEmail(email);

            mockPersonalLogManagerService.Verify(
                s => s.SendPersonalLogToManager(
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<string>(),
                    It.Is<Dictionary<string, string>>(d =>
                        d["account_id"] == "12345")),
                Times.Once);
        }

        [Test]
        public void GivenEmailWithProfiPrizeWonAndValidPrize_WhenProcessing_ThenPrizeIsExtracted()
        {
            AvailableEmail email = BuildAvailableEmail(
                subject: "Profi Prize Won",
                body: "Account: User Account (12345)\nItem: 100 RON Voucher");

            processor.ProcessEmail(email);

            mockPersonalLogManagerService.Verify(
                s => s.SendPersonalLogToManager(
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<string>(),
                    It.Is<Dictionary<string, string>>(d =>
                        d["prize_description"] == "100 RON Voucher")),
                Times.Once);
        }

        [Test]
        public void GivenEmailWithProfiPrizeWonAndNoAccountId_WhenProcessing_ThenAccountIdIsEmpty()
        {
            AvailableEmail email = BuildAvailableEmail(
                subject: "Profi Prize Won",
                body: "Item: 100 RON Voucher");

            processor.ProcessEmail(email);

            mockPersonalLogManagerService.Verify(
                s => s.SendPersonalLogToManager(
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<string>(),
                    It.Is<Dictionary<string, string>>(d =>
                        d["account_id"] == string.Empty)),
                Times.Once);
        }

        [Test]
        public void GivenEmailWithProfiPrizeWonAndNoPrize_WhenProcessing_ThenPrizeIsEmpty()
        {
            AvailableEmail email = BuildAvailableEmail(
                subject: "Profi Prize Won",
                body: "Account: User Account (12345)");

            processor.ProcessEmail(email);

            mockPersonalLogManagerService.Verify(
                s => s.SendPersonalLogToManager(
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<string>(),
                    It.Is<Dictionary<string, string>>(d =>
                        d["prize_description"] == string.Empty)),
                Times.Once);
        }

        [Test]
        public void GivenEmailWithProfiPrizeWonSubject_WhenProcessing_ThenPlatformIsAlwaysProfi()
        {
            AvailableEmail email = BuildAvailableEmail(
                subject: "Profi Prize Won",
                body: "Account: User (9999)\nItem: Test Prize");

            processor.ProcessEmail(email);

            mockPersonalLogManagerService.Verify(
                s => s.SendPersonalLogToManager(
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<string>(),
                    It.Is<Dictionary<string, string>>(d =>
                        d["platform"] == "Profi")),
                Times.Once);
        }

        [Test]
        public void GivenEmailWithoutProfiPrizeWonSubject_WhenProcessing_ThenPersonalLogManagerIsNotCalled()
        {
            AvailableEmail email = BuildAvailableEmail(
                subject: "Account: User Account (12345)\nItem: 100 RON Voucher",
                body: "You have won a prize");

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
                subject: "profi prize won",
                body: "Account: User (1234)\nItem: Prize");

            processor.ProcessEmail(email);

            mockPersonalLogManagerService.Verify(
                s => s.SendPersonalLogToManager(
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, string>>()),
                Times.Once);
        }

        [Test]
        public void GivenEmailWithMultipleAccountIds_WhenProcessing_ThenFirstAccountIdIsExtracted()
        {
            AvailableEmail email = BuildAvailableEmail(
                subject: "Profi Prize Won",
                body: "Account: User (111) and Account: User (222)\nItem: Prize");

            processor.ProcessEmail(email);

            mockPersonalLogManagerService.Verify(
                s => s.SendPersonalLogToManager(
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<string>(),
                    It.Is<Dictionary<string, string>>(d =>
                        d["account_id"] == "111")),
                Times.Once);
        }

        [Test]
        public void GivenEmailWithMultiplePrizes_WhenProcessing_ThenFirstPrizeIsExtracted()
        {
            AvailableEmail email = BuildAvailableEmail(
                subject: "Profi Prize Won",
                body: "Account: User (12345)\nItem: Prize One\nItem: Prize Two");

            processor.ProcessEmail(email);

            mockPersonalLogManagerService.Verify(
                s => s.SendPersonalLogToManager(
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<string>(),
                    It.Is<Dictionary<string, string>>(d =>
                        d["prize_description"] == "Prize One")),
                Times.Once);
        }

        [Test]
        public void GivenEmailWithAccountIdWithLeadingZeros_WhenProcessing_ThenAccountIdIsExtracted()
        {
            AvailableEmail email = BuildAvailableEmail(
                subject: "Profi Prize Won",
                body: "Account: User (00123)\nItem: Prize");

            processor.ProcessEmail(email);

            mockPersonalLogManagerService.Verify(
                s => s.SendPersonalLogToManager(
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<string>(),
                    It.Is<Dictionary<string, string>>(d =>
                        d["account_id"] == "00123")),
                Times.Once);
        }

        [Test]
        public void GivenEmailWithPrizeWithSpecialCharacters_WhenProcessing_ThenPrizeIsExtracted()
        {
            AvailableEmail email = BuildAvailableEmail(
                subject: "Profi Prize Won",
                body: "Account: User (12345)\nItem: 50% Discount - Free Shipping & Gift Card");

            processor.ProcessEmail(email);

            mockPersonalLogManagerService.Verify(
                s => s.SendPersonalLogToManager(
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<string>(),
                    It.Is<Dictionary<string, string>>(d =>
                        d["prize_description"] == "50% Discount - Free Shipping & Gift Card")),
                Times.Once);
        }

        [Test]
        public void GivenEmailWithPrizeWithLeadingAndTrailingWhitespace_WhenProcessing_ThenWhitespaceIsTrimmed()
        {
            AvailableEmail email = BuildAvailableEmail(
                subject: "Profi Prize Won",
                body: "Account: User (12345)\nItem:   Trimmed Prize   ");

            processor.ProcessEmail(email);

            mockPersonalLogManagerService.Verify(
                s => s.SendPersonalLogToManager(
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<string>(),
                    It.Is<Dictionary<string, string>>(d =>
                        d["prize_description"] == "Trimmed Prize")),
                Times.Once);
        }

        [Test]
        public void GivenEmailWithExtraTextInSubject_WhenProcessing_ThenPersonalLogManagerIsCalled()
        {
            AvailableEmail email = BuildAvailableEmail(
                subject: "ALERT: Profi Prize Won - Claim Now!",
                body: "Account: User (12345)\nItem: Prize");

            processor.ProcessEmail(email);

            mockPersonalLogManagerService.Verify(
                s => s.SendPersonalLogToManager(
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, string>>()),
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
