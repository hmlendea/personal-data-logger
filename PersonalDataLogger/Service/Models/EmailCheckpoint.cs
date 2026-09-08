
namespace PersonalDataLogger.Service.Models
{
    public sealed class EmailCheckpoint
    {
        public uint UidValidity { get; init; }

        public uint LastProcessedUid { get; set; }
    }
}