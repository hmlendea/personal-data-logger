using System.Collections.Generic;
using MimeKit;

namespace PersonalDataLogger.Service.Processors
{
    public interface IEmailProcessor
    {
        void LogIn();

        void LogOut();
    }
}
