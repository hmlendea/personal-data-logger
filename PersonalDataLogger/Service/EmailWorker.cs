using System;
using System.IO;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;

using MailKit;
using PersonalDataLogger.Logging;
using NuciLog.Core;
using PersonalDataLogger.Configuration;
using PersonalDataLogger.Service.Models;
using PersonalDataLogger.Service.Processors;

namespace PersonalDataLogger.Service
{
    public class EmailWorker(
        IAliExpressProcessor aliExpressProcessor,
        IDiscordProcessor discordProcessor,
        IGandiProcessor gandiProcessor,
        IGoogleProcessor googleProcessor,
        IOpsGenieEmailProcessor opsGenieEmailProcessor,
        IPayPalProcessor payPalProcessor,
        IProfiProcessor profiProcessor,
        IEmailProcessor emailProcessor,
        ImapSettings imapSettings,
        ILogger logger)
        : IEmailWorker
    {
        static readonly JsonSerializerOptions CheckpointSerializerOptions = new()
        {
            WriteIndented = true
        };

        readonly string checkpointFilePath = Path.Combine(AppContext.BaseDirectory, CheckpointFileName);

        private bool isLoggedIn;

        static string CheckpointFileName => "imap-checkpoint.json";
        static int MinimumRetryDelayMinutes => 1;

        private static TimeSpan PollInterval => TimeSpan.FromSeconds(5);

        private TimeSpan RetryDelay => TimeSpan.FromMinutes(
            Math.Max(MinimumRetryDelayMinutes, imapSettings.RetryDelayMinutes));

        public void WatchEmails()
        {
            EmailCheckpoint checkpoint = LoadCheckpoint();

            logger.Info(
                MyOperation.WatchEmails,
                OperationStatus.Started,
                "Listening for incoming emails.");

            try
            {
                while (true)
                {
                    checkpoint = ProcessAvailableEmails(checkpoint);

                    WaitForNextPoll();
                }
            }
            catch (Exception exception)
            {
                logger.Error(
                    MyOperation.WatchEmails,
                    OperationStatus.Failure,
                    exception);

                throw;
            }
            finally
            {
                LogOut();
            }
        }

        protected virtual void WaitForNextPoll() => Thread.Sleep(PollInterval);

        protected virtual void WaitBeforeRetry(TimeSpan retryDelay) => Thread.Sleep(retryDelay);

        private static bool IsConnectionFailure(Exception exception)
            => exception is IOException or SocketException or TimeoutException or
                ProtocolException or ServiceNotConnectedException or
                ServiceNotAuthenticatedException or FolderNotOpenException or
                ObjectDisposedException;

        private AvailableEmailBatch GetAvailableEmails(uint lastProcessedUid)
        {
            while (true)
            {
                try
                {
                    if (!isLoggedIn)
                    {
                        emailProcessor.LogIn();
                        isLoggedIn = true;
                    }

                    return emailProcessor.GetAvailableEmails(lastProcessedUid);
                }
                catch (Exception exception) when (IsConnectionFailure(exception))
                {
                    isLoggedIn = false;
                    logger.Warn(
                        MyOperation.WatchEmails,
                        OperationStatus.InProgress,
                        "The IMAP connection failed. Reconnecting after the configured retry delay.",
                        exception);
                }

                WaitBeforeRetry(RetryDelay);
            }
        }

        private void LogOut()
        {
            isLoggedIn = false;

            try
            {
                emailProcessor.LogOut();
            }
            catch (Exception exception)
            {
                logger.Warn(
                    MyOperation.WatchEmails,
                    OperationStatus.InProgress,
                    "Failed to log out of the IMAP server. The connection is dropped regardless.",
                    exception);
            }
        }

        EmailCheckpoint ProcessAvailableEmails(EmailCheckpoint checkpoint)
        {
            AvailableEmailBatch batch = GetAvailableEmails(checkpoint.LastProcessedUid);

            if (checkpoint.UidValidity != batch.UidValidity)
            {
                checkpoint = new EmailCheckpoint
                {
                    UidValidity = batch.UidValidity,
                    LastProcessedUid = 0
                };

                SaveCheckpoint(checkpoint);
                batch = GetAvailableEmails(checkpoint.LastProcessedUid);
            }

            DateTime minAllowedDate = DateTime.UtcNow.AddSeconds(-Math.Max(0, imapSettings.MaxEmailAge));

            foreach (AvailableEmail email in batch.Emails)
            {
                if (email.Timestamp.UtcDateTime >= minAllowedDate)
                {
                    logger.Info(
                        MyOperation.WatchEmails,
                        OperationStatus.InProgress,
                        new LogInfo(MyLogInfoKey.Uid, email.Uid),
                        new LogInfo(MyLogInfoKey.Subject, email.Subject),
                        new LogInfo(MyLogInfoKey.Date, email.Timestamp));

                    if (email.Sender.Contains("aliexpress"))
                    {
                        aliExpressProcessor.ProcessEmail(email);
                    }
                    else if (email.Sender.Contains("discord.com"))
                    {
                        discordProcessor.ProcessEmail(email);
                    }
                    else if (email.Sender.Contains("gandi.net") ||
                             email.Sender.Contains("gandi_net"))
                    {
                        gandiProcessor.ProcessEmail(email);
                    }
                    else if (email.Sender.Contains("google.com") ||
                             email.Sender.Contains("accounts.google.com"))
                    {
                        googleProcessor.ProcessEmail(email);
                    }
                    else if (email.Sender.Contains("opsgenie"))
                    {
                        opsGenieEmailProcessor.ProcessEmail(email);
                    }
                    else if (email.Sender.Contains("paypal"))
                    {
                        payPalProcessor.ProcessEmail(email);
                    }
                    else if (email.Sender.Contains("profi_bot_server"))
                    {
                        profiProcessor.ProcessEmail(email);
                    }
                }

                checkpoint.LastProcessedUid = email.Uid;
                SaveCheckpoint(checkpoint);
            }

            return checkpoint;
        }

        EmailCheckpoint LoadCheckpoint()
        {
            try
            {
                if (!File.Exists(checkpointFilePath))
                {
                    return new EmailCheckpoint();
                }

                string json = File.ReadAllText(checkpointFilePath);
                EmailCheckpoint checkpoint = JsonSerializer.Deserialize<EmailCheckpoint>(json);

                return checkpoint ?? new EmailCheckpoint();
            }
            catch (Exception ex)
            {
                logger.Warn(
                    MyOperation.WatchEmails,
                    OperationStatus.InProgress,
                    "Failed to load the email checkpoint. Falling back to a fresh checkpoint.",
                    ex);

                return new EmailCheckpoint();
            }
        }

        void SaveCheckpoint(EmailCheckpoint checkpoint)
        {
            try
            {
                string json = JsonSerializer.Serialize(checkpoint, CheckpointSerializerOptions);

                File.WriteAllText(checkpointFilePath, json);
            }
            catch (Exception exception)
            {
                logger.Error(
                    MyOperation.WatchEmails,
                    OperationStatus.Failure,
                    "Failed to persist the email checkpoint.",
                    exception);
            }
        }
    }
}
