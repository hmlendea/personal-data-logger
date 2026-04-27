using NuciLog.Core;

namespace PersonalDataLogger.Logging
{
    public sealed class MyLogInfoKey : LogInfoKey
    {
        MyLogInfoKey(string name)
            : base(name)
        {

        }

        public static LogInfoKey Data => new MyLogInfoKey(nameof(Data));
        public static LogInfoKey Date => new MyLogInfoKey(nameof(Date));
        public static LogInfoKey MaxAge => new MyLogInfoKey(nameof(MaxAge));
        public static LogInfoKey Password => new MyLogInfoKey(nameof(Password));
        public static LogInfoKey Port => new MyLogInfoKey(nameof(Port));
        public static LogInfoKey ResponseCode => new MyLogInfoKey(nameof(ResponseCode));
        public static LogInfoKey Server => new MyLogInfoKey(nameof(Server));
        public static LogInfoKey Subject => new MyLogInfoKey(nameof(Subject));
        public static LogInfoKey Template => new MyLogInfoKey(nameof(Template));
        public static LogInfoKey Time => new MyLogInfoKey(nameof(Time));
        public static LogInfoKey TimeZone => new MyLogInfoKey(nameof(TimeZone));
        public static LogInfoKey Uid => new MyLogInfoKey(nameof(Uid));
        public static LogInfoKey Username => new MyLogInfoKey(nameof(Username));
    }
}