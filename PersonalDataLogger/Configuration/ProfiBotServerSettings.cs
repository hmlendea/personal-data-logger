namespace PersonalDataLogger.Configuration
{
    public sealed class ProfiBotServerSettings
    {
        public string AccountName { get; set; }

        public string AccountsEndpoint { get; set; }

        public string BaseUrl { get; set; }

        public string ScheduledHours { get; set; } = "06:30";

        public string ClientId { get; set; }

        public string HmacSharedSecretKey { get; set; }

        public string UserApiKey { get; set; }

        public string Username { get; set; }

        public bool IsConfigured =>
            SettingValue.IsResolved(AccountName) &&
            SettingValue.IsResolved(AccountsEndpoint) &&
            SettingValue.IsResolved(BaseUrl) &&
            SettingValue.IsResolved(ClientId) &&
            SettingValue.IsResolved(HmacSharedSecretKey) &&
            SettingValue.IsResolved(UserApiKey) &&
            SettingValue.IsResolved(Username);
    }
}