using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;

using MailKit;
using MailKit.Net.Imap;
using MailKit.Security;

using Moq;
using Moq.Protected;

using NuciLog.Core;

using NUnit.Framework;

using PersonalDataLogger.Configuration;
using PersonalDataLogger.Service;
using PersonalDataLogger.Service.Processors;

namespace PersonalDataLogger.UnitTests.Service
{
    [TestFixture]
    public class EmailWorkerTests
    {
        private Mock<IEmailProcessor> emailProcessor;
        private Mock<EmailWorker> emailWorker;

        private static IEnumerable<Exception> ConnectionFailures =>
        [
            new IOException("The connection timed out."),
            new SocketException((int)SocketError.ConnectionReset),
            new ImapProtocolException("The server closed the connection."),
            new ServiceNotConnectedException("The client is disconnected.")
        ];

        [SetUp]
        public void SetUp()
        {
            emailProcessor = new();
            emailWorker = new(
                Mock.Of<IAliExpressProcessor>(),
                Mock.Of<IGandiProcessor>(),
                Mock.Of<IOpsGenieEmailProcessor>(),
                Mock.Of<IPayPalProcessor>(),
                Mock.Of<IProfiProcessor>(),
                emailProcessor.Object,
                new ImapSettings(),
                Mock.Of<ILogger>())
            {
                CallBase = true
            };
            emailWorker.Protected().Setup("WaitForNextPoll");
        }

        [TestCaseSource(nameof(ConnectionFailures))]
        public void GivenAFetchConnectionFailure_WhenWatchingEmails_ThenReconnectsWithTheSameUid(
            Exception connectionFailure)
        {
            List<uint> requestedUids = [];
            InvalidOperationException terminalFailure = new("The mailbox is unavailable.");
            emailProcessor
                .Setup(processor => processor.GetAvailableEmails(It.IsAny<uint>()))
                .Callback<uint>(requestedUids.Add)
                .Returns((uint lastProcessedUid) =>
                {
                    if (requestedUids.Count == 1)
                    {
                        throw connectionFailure;
                    }

                    throw terminalFailure;
                });

            Assert.That(
                () => emailWorker.Object.WatchEmails(),
                Throws.Exception.SameAs(terminalFailure));
            Assert.That(requestedUids, Has.Count.EqualTo(2));
            Assert.That(requestedUids[1], Is.EqualTo(requestedUids[0]));
            emailProcessor.Verify(processor => processor.LogIn(), Times.Exactly(2));
            emailProcessor.Verify(processor => processor.LogOut(), Times.Once);
            emailWorker.Protected().Verify("WaitForNextPoll", Times.Once());
        }

        [Test]
        public void GivenAFailedReconnection_WhenWatchingEmails_ThenRetriesConnectingAgain()
        {
            InvalidOperationException terminalFailure = new("The mailbox is unavailable.");
            emailProcessor.SetupSequence(processor => processor.LogIn())
                .Pass()
                .Throws(new IOException("The connection timed out."))
                .Pass();
            emailProcessor.SetupSequence(processor => processor.GetAvailableEmails(It.IsAny<uint>()))
                .Throws(new IOException("The connection timed out."))
                .Throws(terminalFailure);

            Assert.That(
                () => emailWorker.Object.WatchEmails(),
                Throws.Exception.SameAs(terminalFailure));
            emailProcessor.Verify(processor => processor.LogIn(), Times.Exactly(3));
            emailProcessor.Verify(
                processor => processor.GetAvailableEmails(It.IsAny<uint>()),
                Times.Exactly(2));
            emailWorker.Protected().Verify("WaitForNextPoll", Times.Exactly(2));
        }

        [TestCaseSource(nameof(ConnectionFailures))]
        public void GivenAnInitialConnectionFailure_WhenWatchingEmails_ThenRetriesLoggingIn(
            Exception connectionFailure)
        {
            InvalidOperationException terminalFailure = new("The mailbox is unavailable.");
            emailProcessor.SetupSequence(processor => processor.LogIn())
                .Throws(connectionFailure)
                .Pass();
            emailProcessor.Setup(processor => processor.GetAvailableEmails(It.IsAny<uint>()))
                .Throws(terminalFailure);

            Assert.That(
                () => emailWorker.Object.WatchEmails(),
                Throws.Exception.SameAs(terminalFailure));
            emailProcessor.Verify(processor => processor.LogIn(), Times.Exactly(2));
            emailProcessor.Verify(
                processor => processor.GetAvailableEmails(It.IsAny<uint>()),
                Times.Once);
            emailWorker.Protected().Verify("WaitForNextPoll", Times.Once());
        }

        [Test]
        public void GivenAnAuthenticationFailure_WhenWatchingEmails_ThenDoesNotRetry()
        {
            AuthenticationException failure = new("The credentials were rejected.");
            emailProcessor.Setup(processor => processor.LogIn()).Throws(failure);

            Assert.That(() => emailWorker.Object.WatchEmails(), Throws.Exception.SameAs(failure));
            emailProcessor.Verify(processor => processor.LogIn(), Times.Once);
            emailProcessor.Verify(
                processor => processor.GetAvailableEmails(It.IsAny<uint>()),
                Times.Never);
            emailWorker.Protected().Verify("WaitForNextPoll", Times.Never());
        }

        [Test]
        public void GivenANonConnectionFetchFailure_WhenWatchingEmails_ThenDoesNotRetry()
        {
            InvalidOperationException failure = new("The mailbox is unavailable.");
            emailProcessor.Setup(processor => processor.GetAvailableEmails(It.IsAny<uint>()))
                .Throws(failure);

            Assert.That(() => emailWorker.Object.WatchEmails(), Throws.Exception.SameAs(failure));
            emailProcessor.Verify(processor => processor.LogIn(), Times.Once);
            emailProcessor.Verify(
                processor => processor.GetAvailableEmails(It.IsAny<uint>()),
                Times.Once);
            emailWorker.Protected().Verify("WaitForNextPoll", Times.Never());
        }

        [Test]
        public void GivenAPreviouslyStoppedWorker_WhenWatchingEmailsAgain_ThenLogsInAgain()
        {
            InvalidOperationException failure = new("The mailbox is unavailable.");
            emailProcessor.Setup(processor => processor.GetAvailableEmails(It.IsAny<uint>()))
                .Throws(failure);

            Assert.That(() => emailWorker.Object.WatchEmails(), Throws.Exception.SameAs(failure));
            Assert.That(() => emailWorker.Object.WatchEmails(), Throws.Exception.SameAs(failure));
            emailProcessor.Verify(processor => processor.LogIn(), Times.Exactly(2));
            emailProcessor.Verify(processor => processor.LogOut(), Times.Exactly(2));
            emailWorker.Protected().Verify("WaitForNextPoll", Times.Never());
        }
    }
}