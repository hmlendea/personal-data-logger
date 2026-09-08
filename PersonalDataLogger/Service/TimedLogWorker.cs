using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using NuciLog.Core;

using PersonalDataLogger.Logging;

namespace PersonalDataLogger.Service
{
    public sealed class TimedLogWorker(
        IEnumerable<ITimedLog> timedLogs,
        ILogger logger)
        : ITimedLogWorker
    {
        public void WatchTimedLogs()
        {
            IEnumerable<Task> timedLogTasks = timedLogs.Select(
                timedLog => Task.Run(() => WatchTimedLog(timedLog)));
            Task completedTask = Task.WhenAny(timedLogTasks).GetAwaiter().GetResult();

            completedTask.GetAwaiter().GetResult();
        }

        private void WatchTimedLog(ITimedLog timedLog)
        {
            try
            {
                while (true)
                {
                    DateTimeOffset currentTime = DateTimeOffset.Now;
                    DateTimeOffset nextExecution = timedLog.GetNextExecution(currentTime);
                    TimeSpan delay = nextExecution - currentTime;

                    if (delay > TimeSpan.Zero)
                    {
                        Thread.Sleep(delay);
                    }

                    timedLog.Execute().GetAwaiter().GetResult();
                }
            }
            catch (Exception exception)
            {
                logger.Error(
                    MyOperation.ExecuteTimedLog,
                    OperationStatus.Failure,
                    exception);

                throw;
            }
        }
    }
}