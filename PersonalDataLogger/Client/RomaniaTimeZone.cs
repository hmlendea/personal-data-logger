using System;

namespace PersonalDataLogger.Client
{
    public static class RomaniaTimeZone
    {
        private static string RomaniaTimeZoneId => "Europe/Bucharest";
        private static string WindowsRomaniaTimeZoneId => "GTB Standard Time";

        private static TimeZoneInfo TimeZone => GetTimeZone();

        public static DateTimeOffset ConvertToRomanianTime(DateTimeOffset dateTime)
            => TimeZoneInfo.ConvertTime(dateTime, TimeZone);

        public static DateTimeOffset GetRomanianTimestamp(DateOnly date, TimeOnly? time)
        {
            TimeOnly localTime = TimeOnly.MinValue;

            if (time is not null)
            {
                localTime = time.Value;
            }

            DateTime localDateTime = date.ToDateTime(localTime, DateTimeKind.Unspecified);

            return new DateTimeOffset(localDateTime, TimeZone.GetUtcOffset(localDateTime));
        }

        private static TimeZoneInfo GetTimeZone()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(RomaniaTimeZoneId);
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.FindSystemTimeZoneById(WindowsRomaniaTimeZoneId);
            }
        }
    }
}