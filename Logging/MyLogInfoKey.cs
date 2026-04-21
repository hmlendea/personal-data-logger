using NuciLog.Core;

namespace PersonalDataLogger.Logging
{
    public sealed class MyLogInfoKey : LogInfoKey
    {
        MyLogInfoKey(string name)
            : base(name)
        {

        }

        public static LogInfoKey Server => new MyLogInfoKey(nameof(Server));

        public static LogInfoKey Port => new MyLogInfoKey(nameof(Port));

        public static LogInfoKey Username => new MyLogInfoKey(nameof(Username));

        public static LogInfoKey Password => new MyLogInfoKey(nameof(Password));

        public static LogInfoKey MaxAge => new MyLogInfoKey(nameof(MaxAge));

        public static LogInfoKey Uid => new MyLogInfoKey(nameof(Uid));

        public static LogInfoKey Subject => new MyLogInfoKey(nameof(Subject));

        public static LogInfoKey Date => new MyLogInfoKey(nameof(Date));
    }
}