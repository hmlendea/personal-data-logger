using PersonalDataLogger.Service.Models;

namespace PersonalDataLogger.Service.Processors
{
    public interface IEmailProcessor
    {
        void LogIn();

        AvailableEmailBatch GetAvailableEmails(uint lastProcessedUid);

        void LogOut();
    }
}
