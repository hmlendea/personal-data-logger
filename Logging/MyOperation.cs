using NuciLog.Core;

namespace PersonalDataLogger.Logging
{
    public sealed class MyOperation : Operation
    {
        MyOperation(string name)
            : base(name)
        {

        }

        public static Operation EmailLogIn => new MyOperation(nameof(EmailLogIn));
        public static Operation EmailLogOut => new MyOperation(nameof(EmailLogOut));
        public static Operation ExecuteTimedLog => new MyOperation(nameof(ExecuteTimedLog));
        public static Operation ProcessEmail => new MyOperation(nameof(ProcessEmail));
        public static Operation StoreLog => new MyOperation(nameof(StoreLog));
        public static Operation WatchEmails => new MyOperation(nameof(WatchEmails));
    }
}