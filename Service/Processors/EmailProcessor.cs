using System;
using System.Collections.Generic;
using System.Linq;

using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MimeKit;
using NuciLog.Core;
using PersonalDataLogger.Configuration;
using PersonalDataLogger.Logging;
using PersonalDataLogger.Service.Models;

namespace PersonalDataLogger.Service.Processors
{
    public sealed class EmailProcessor(
        ImapSettings imapSettings,
        ILogger logger) : IEmailProcessor
    {
        readonly ImapSettings imapSettings = imapSettings;
        readonly ILogger logger = logger;
        readonly ImapClient imapClient = new();

        public void LogIn()
        {
            IEnumerable<LogInfo> logInfos =
            [
                new(MyLogInfoKey.Server, imapSettings.Server),
                new(MyLogInfoKey.Port, imapSettings.Port)
            ];

            logger.Info(
                MyOperation.EmailLogIn,
                OperationStatus.Started,
                "Connecting to the IMAP server.",
                logInfos);

            try
            {
                imapClient.Connect(imapSettings.Server, imapSettings.Port, true);
            }
            catch (Exception ex)
            {
                logger.Error(
                    MyOperation.EmailLogIn,
                    OperationStatus.Failure,
                    "Failed to connect to the IMAP server.",
                    ex,
                    logInfos);

                throw;
            }

            logInfos = logInfos.Append(new(MyLogInfoKey.Username, imapSettings.Username));

            logger.Info(
                MyOperation.EmailLogIn,
                OperationStatus.InProgress,
                "Authenticating on the IMAP server.",
                logInfos);

            try
            {
                imapClient.Authenticate(imapSettings.Username, imapSettings.Password);
            }
            catch (Exception ex)
            {
                logger.Error(
                    MyOperation.EmailLogIn,
                    OperationStatus.Failure,
                    "Failed to authenticate on the IMAP server.",
                    ex,
                    logInfos);

                throw;
            }

            logger.Info(
                MyOperation.EmailLogIn,
                OperationStatus.Success,
                "Logged into the IMAP server.",
                logInfos);
        }

        public void LogOut()
        {
            IEnumerable<LogInfo> logInfos =
            [
                new(MyLogInfoKey.Server, imapSettings.Server),
                new(MyLogInfoKey.Port, imapSettings.Port),
                new(MyLogInfoKey.Username, imapSettings.Username)
            ];

            logger.Info(
                MyOperation.EmailLogOut,
                OperationStatus.Started,
                "Disconnecting from the IMAP server.",
                logInfos);

            try
            {
                imapClient.Disconnect(true);
            }
            catch (Exception ex)
            {
                logger.Error(
                    MyOperation.EmailLogOut,
                    OperationStatus.Failure,
                    "Failed to disconnect from the IMAP server.",
                    ex,
                    logInfos);

                throw;
            }

            imapClient.Dispose();

            logger.Info(
                MyOperation.EmailLogOut,
                OperationStatus.Success,
                "Logged out of the IMAP server.",
                logInfos);
        }

        public AvailableEmailBatch GetAvailableEmails(uint lastProcessedUid)
        {
            IMailFolder inbox = imapClient.Inbox;

            if (!inbox.IsOpen)
            {
                inbox.Open(FolderAccess.ReadOnly);
            }

            IList<UniqueId> allUids = inbox.Search(SearchQuery.All);

            IReadOnlyList<AvailableEmail> emails = allUids
                .Where(x => x.Id > lastProcessedUid)
                .OrderBy(x => x.Id)
                .Select(uid =>
                {
                    MimeMessage message = inbox.GetMessage(uid);

                    return new AvailableEmail
                    {
                        Uid = uid.Id,
                        Date = message.Date,
                        Sender = message.From.ToString(),
                        Subject = message.Subject ?? string.Empty,
                        Body = message.TextBody ?? string.Empty
                    };
                })
                .ToArray();

            return new AvailableEmailBatch
            {
                UidValidity = inbox.UidValidity,
                Emails = emails
            };
        }
    }
}
