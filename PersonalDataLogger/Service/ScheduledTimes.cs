using System;
using System.Collections.Generic;
using System.Globalization;

namespace PersonalDataLogger.Service
{
    public sealed class ScheduledTimes
    {
        private static string EmptyConfigurationMessage =>
            "The scheduled hours configuration is empty. Use one or more times in the HH:mm format.";

        private readonly List<TimeSpan> times;

        public ScheduledTimes(string scheduledHours)
        {
            times = ParseTimes(scheduledHours);
        }

        public DateTimeOffset GetNextExecution(DateTimeOffset currentTime)
        {
            DateTime localCurrentTime = currentTime.LocalDateTime;
            DateTime nextExecution = localCurrentTime.Date.Add(times[0]);

            foreach (TimeSpan scheduledTime in times)
            {
                DateTime candidate = localCurrentTime.Date.Add(scheduledTime);

                if (candidate > localCurrentTime)
                {
                    nextExecution = candidate;
                    break;
                }
            }

            if (nextExecution <= localCurrentTime)
            {
                nextExecution = nextExecution.AddDays(1);
            }

            TimeSpan localOffset = TimeZoneInfo.Local.GetUtcOffset(nextExecution);

            return new DateTimeOffset(nextExecution, localOffset);
        }

        private static List<TimeSpan> ParseTimes(string scheduledHours)
        {
            if (string.IsNullOrWhiteSpace(scheduledHours))
            {
                throw new FormatException(EmptyConfigurationMessage);
            }

            string[] configuredTimes = scheduledHours.Split(',', StringSplitOptions.TrimEntries);
            List<TimeSpan> parsedTimes = [];

            foreach (string configuredTime in configuredTimes)
            {
                if (!TimeSpan.TryParseExact(
                    configuredTime,
                    ["h\\:mm", "hh\\:mm"],
                    CultureInfo.InvariantCulture,
                    out TimeSpan parsedTime) ||
                    parsedTime < TimeSpan.Zero ||
                    parsedTime >= TimeSpan.FromDays(1))
                {
                    throw new FormatException(
                        $"The scheduled time '{configuredTime}' is invalid. Use the HH:mm format.");
                }

                parsedTimes.Add(parsedTime);
            }

            parsedTimes.Sort();

            return parsedTimes;
        }
    }
}