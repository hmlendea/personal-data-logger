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
        private Mock<IEmailProcessor> emailProcessor = null!;
        private Mock<EmailWorker> emailWorker = null!;

        private static IEnumerable<Exception> ConnectionFailures =>
        [
            new IOException("The connection timed out."),
            new SocketException((int)SocketError.ConnectionReset),
            new TimeoutException("The operation has timed out."),
            new ImapProtocolException("The server closed the connection."),
            new ServiceNotConnectedException("The client is disconnected."),
            new ServiceNotAuthenticatedException("The client is not authenticated."),
            new FolderNotOpenException("Inbox", FolderAccess.ReadOnly),
            new ObjectDisposedException("ImapClient")
        ];

        [SetUp]
        public void SetUp()
        {
            emailProcessor = new();
            emailWorker = new(
                Mock.Of<IAliExpressProcessor>(),
                Mock.Of<IDiscordProcessor>(),
                Mock.Of<IGandiProcessor>(),
                Mock.Of<IGoogleProcessor>(),
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
            emailWorker.Protected().Setup("WaitBeforeRetry", ItExpr.IsAny<TimeSpan>());
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
            emailWorker.Protected().Verify("WaitBeforeRetry", Times.Once(), ItExpr.IsAny<TimeSpan>());
            emailWorker.Protected().Verify("WaitForNextPoll", Times.Never());
        }

        [Test]
        public void GivenAConfiguredRetryDelay_WhenReconnecting_ThenWaitsTheConfiguredDelay()
        {
            List<TimeSpan> retryDelays = [];
            Mock<EmailWorker> workerWithDelay = BuildWorkerWithRetryDelay(12, retryDelays);

            InvalidOperationException terminalFailure = new("The mailbox is unavailable.");
            emailProcessor
                .SetupSequence(processor => processor.GetAvailableEmails(It.IsAny<uint>()))
                .Throws(new TimeoutException("The operation has timed out."))
                .Throws(terminalFailure);

            Assert.That(
                () => workerWithDelay.Object.WatchEmails(),
                Throws.Exception.SameAs(terminalFailure));
            Assert.That(retryDelays, Is.EqualTo(new[] { TimeSpan.FromMinutes(12) }));
        }

        [TestCase(0)]
        [TestCase(-30)]
        public void GivenAnInvalidRetryDelay_WhenReconnecting_ThenWaitsTheMinimumDelay(
            int retryDelayMinutes)
        {
            List<TimeSpan> retryDelays = [];
            Mock<EmailWorker> workerWithDelay = BuildWorkerWithRetryDelay(
                retryDelayMinutes,
                retryDelays);

            InvalidOperationException terminalFailure = new("The mailbox is unavailable.");
            emailProcessor
                .SetupSequence(processor => processor.GetAvailableEmails(It.IsAny<uint>()))
                .Throws(new TimeoutException("The operation has timed out."))
                .Throws(terminalFailure);

            Assert.That(
                () => workerWithDelay.Object.WatchEmails(),
                Throws.Exception.SameAs(terminalFailure));
            Assert.That(retryDelays, Is.EqualTo(new[] { TimeSpan.FromMinutes(1) }));
        }

        [Test]
        public void GivenADisconnectedClient_WhenTheWatchLoopEnds_ThenTheFailureIsNotMaskedByLogOut()
        {
            InvalidOperationException terminalFailure = new("The mailbox is unavailable.");
            emailProcessor.Setup(processor => processor.GetAvailableEmails(It.IsAny<uint>()))
                .Throws(terminalFailure);
            emailProcessor.Setup(processor => processor.LogOut())
                .Throws(new ServiceNotConnectedException("The client is disconnected."));

            Assert.That(
                () => emailWorker.Object.WatchEmails(),
                Throws.Exception.SameAs(terminalFailure));
            emailProcessor.Verify(processor => processor.LogOut(), Times.Once);
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
            emailWorker.Protected().Verify("WaitBeforeRetry", Times.Exactly(2), ItExpr.IsAny<TimeSpan>());
            emailWorker.Protected().Verify("WaitForNextPoll", Times.Never());
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
            emailWorker.Protected().Verify("WaitBeforeRetry", Times.Once(), ItExpr.IsAny<TimeSpan>());
            emailWorker.Protected().Verify("WaitForNextPoll", Times.Never());
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
            emailWorker.Protected().Verify("WaitBeforeRetry", Times.Never(), ItExpr.IsAny<TimeSpan>());
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
            emailWorker.Protected().Verify("WaitBeforeRetry", Times.Never(), ItExpr.IsAny<TimeSpan>());
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

        private Mock<EmailWorker> BuildWorkerWithRetryDelay(
            int retryDelayMinutes,
            List<TimeSpan> retryDelays)
        {
            Mock<EmailWorker> workerWithDelay = new(
                Mock.Of<IAliExpressProcessor>(),
                Mock.Of<IDiscordProcessor>(),
                Mock.Of<IGandiProcessor>(),
                Mock.Of<IGoogleProcessor>(),
                Mock.Of<IOpsGenieEmailProcessor>(),
                Mock.Of<IPayPalProcessor>(),
                Mock.Of<IProfiProcessor>(),
                emailProcessor.Object,
                new ImapSettings { RetryDelayMinutes = retryDelayMinutes },
                Mock.Of<ILogger>())
            {
                CallBase = true
            };
            workerWithDelay.Protected().Setup("WaitForNextPoll");
            workerWithDelay.Protected()
                .Setup("WaitBeforeRetry", ItExpr.IsAny<TimeSpan>())
                .Callback<TimeSpan>(retryDelays.Add);

            return workerWithDelay;
        }
    }
}