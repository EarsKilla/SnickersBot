using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

namespace SnickersBot.Models
{
    public class Coupon
    {
        [JsonProperty("id")]
        public string Id { get; protected set; }
        [JsonProperty("date")]
        public string Date { get; protected set; }
        [JsonProperty("time")]
        [JsonConverter(typeof(UnixDateTimeConverter))]
        public DateTimeOffset? Time { get; protected set; }
        [JsonProperty("timeslot")]
        public string TimeSlot { get; protected set; }
        [JsonProperty("code")]
        public string Code { get; protected set; }
        [JsonProperty("value")]
        public int Value { get; protected set; }
    }
}
