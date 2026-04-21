using System;
using PersonalDataLogger.Logging;
using NuciLog.Core;
using PersonalDataLogger.Service.Processors;

namespace PersonalDataLogger.Service
{
    public class EmailWatcher(
        IEmailProcessor emailProcessor,
        ILogger logger)
        : IEmailWatcher
    {
        public void WatchEmails()
        {
            emailProcessor.LogIn();

            logger.Info(
                MyOperation.WatchEmails,
                OperationStatus.Started,
                "Listening for incoming household update requests.");

            try
            {
                while(true)
                {
                    // TODO: Get and process emails
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
                emailProcessor.LogOut();
            }
        }
    }
}
