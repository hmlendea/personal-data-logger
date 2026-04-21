using System;
using System.Collections.Generic;

namespace PersonalDataLogger.Service.Models
{
    public sealed class AvailableEmailBatch
    {
        public uint UidValidity { get; init; }

        public IReadOnlyList<AvailableEmail> Emails { get; init; } = Array.Empty<AvailableEmail>();
    }
}