namespace PersonalDataLogger.Configuration
{
    public sealed class SteamSettings
    {
        public string ApiKey { get; set; }

        public string Username { get; set; }

        public string ScheduledHours { get; set; } = "07:00";

        public bool IsConfigured =>
            SettingValue.IsResolved(ApiKey) &&
            SettingValue.IsResolved(Username);
    }
}