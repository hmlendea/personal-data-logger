using System;
using System.Threading.Tasks;

namespace PersonalDataLogger.Service
{
    public interface ITimedLog
    {
        DateTimeOffset GetNextExecution(DateTimeOffset currentTime);

        Task Execute();
    }
}