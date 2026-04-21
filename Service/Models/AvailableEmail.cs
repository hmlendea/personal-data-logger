using System;

namespace PersonalDataLogger.Service.Models
{
    public sealed class AvailableEmail
    {
        public uint Uid { get; init; }

        public DateTimeOffset Timestamp { get; init; }

        public string Sender { get; init; } = string.Empty;

        public string Subject { get; init; } = string.Empty;

        public string Body { get; init; } = string.Empty;
    }
}