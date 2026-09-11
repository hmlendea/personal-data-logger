namespace PersonalDataLogger.Configuration
{
    public sealed class ProfiBotServerSettings
    {
        public string AccountName { get; set; }

        public string AccountsEndpoint { get; set; }

        public string BaseUrl { get; set; }

        public string ClientId { get; set; }

        public string HmacSharedSecretKey { get; set; }

        public string UserApiKey { get; set; }

        public string Username { get; set; }

        public bool IsConfigured =>
            IsConfiguredValue(AccountName) &&
            IsConfiguredValue(AccountsEndpoint) &&
            IsConfiguredValue(BaseUrl) &&
            IsConfiguredValue(ClientId) &&
            IsConfiguredValue(HmacSharedSecretKey) &&
            IsConfiguredValue(UserApiKey) &&
            IsConfiguredValue(Username);

        private static bool IsConfiguredValue(string value)
            => !string.IsNullOrWhiteSpace(value) &&
               !value.StartsWith("[[", System.StringComparison.Ordinal) &&
               !value.EndsWith("]]", System.StringComparison.Ordinal);
    }
}