using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using NuciAPI.Requests;
using NuciSecurity.HMAC;

namespace PersonalDataLogger.Client
{
    public class StoreLogRequest : NuciApiRequest
    {
        [Required]
        [HmacOrder(1)]
        public string Date { get; set; }

        [HmacOrder(2)]
        public string Time { get; set; }

        [HmacOrder(3)]
        public string TimeZone { get; set; }

        [HmacOrder(4)]
        public string Template { get; set; }

        [HmacOrder(5)]
        public Dictionary<string, string> Data { get; set; }
    }
}
