using System;

namespace PersonalDataLogger.Service.Models
{
    public sealed class AvailableEmail
    {
        public uint Uid { get; init; }

        public DateTimeOffset Date { get; init; }

        public string Sender { get; init; } = string.Empty;

        public string Subject { get; init; } = string.Empty;

        public string Body { get; init; } = string.Empty;
    }
}