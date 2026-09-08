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
    }
}