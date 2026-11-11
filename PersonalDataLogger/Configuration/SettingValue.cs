using System;

namespace PersonalDataLogger.Configuration
{
    public static class SettingValue
    {
        public static bool IsResolved(string value)
            => !string.IsNullOrWhiteSpace(value) &&
               !value.StartsWith("[[", StringComparison.Ordinal) &&
               !value.EndsWith("]]", StringComparison.Ordinal);
    }
}