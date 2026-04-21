namespace PersonalDataLogger.Configuration
{
    public sealed class PersonalLogManagerSettings
    {
        public string BaseUrl { get; set; }

        public string ApiKey { get; set; }

        public string HmacSharedSecretKey { get; set; }

        public string ClientId { get; set; }
    }
}
