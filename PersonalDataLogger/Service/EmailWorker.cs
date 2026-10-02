using System;
using System.IO;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;

using MailKit;
using MailKit.Net.Imap;
using PersonalDataLogger.Logging;
using NuciLog.Core;
using PersonalDataLogger.Configuration;
using PersonalDataLogger.Service.Models;
using PersonalDataLogger.Service.Processors;

namespace PersonalDataLogger.Service
{
    public class EmailWorker(
        IAliExpressProcessor aliExpressProcessor,
        IGandiProcessor gandiProcessor,
        IOpsGenieEmailProcessor opsGenieEmailProcessor,
        IPayPalProcessor payPalProcessor,
        IProfiProcessor profiProcessor,
        IEmailProcessor emailProcessor,
        ImapSettings imapSettings,
        ILogger logger)
        : IEmailWorker
    {
        const string CheckpointFileName = "imap-checkpoint.json";

        readonly string checkpointFilePath = Path.Combine(AppContext.BaseDirectory, CheckpointFileName);

        private bool isLoggedIn;

        private static TimeSpan PollInterval => TimeSpan.FromSeconds(5);

        public void WatchEmails()
        {
            EmailCheckpoint checkpoint = LoadCheckpoint();

            logger.Info(
                MyOperation.WatchEmails,
                OperationStatus.Started,
                "Listening for incoming emails.");

            try
            {
                while(true)
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
                isLoggedIn = false;
                emailProcessor.LogOut();
            }
        }

        protected virtual void WaitForNextPoll() => Thread.Sleep(PollInterval);

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
                catch (Exception exception) when (
                    exception is IOException or SocketException or
                    ImapProtocolException or ServiceNotConnectedException)
                {
                    isLoggedIn = false;
                    logger.Warn(
                        MyOperation.WatchEmails,
                        OperationStatus.InProgress,
                        "The IMAP connection failed. Reconnecting after the polling delay.",
                        exception);
                }

                WaitForNextPoll();
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
                    else if (email.Sender.Contains("gandi.net") ||
                             email.Sender.Contains("gandi_net"))
                    {
                        gandiProcessor.ProcessEmail(email);
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
            string json = JsonSerializer.Serialize(checkpoint, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(checkpointFilePath, json);
        }
    }
}
