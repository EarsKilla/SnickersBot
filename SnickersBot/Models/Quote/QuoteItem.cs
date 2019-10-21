using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

namespace SnickersBot.Models.Quote
{
    public class QuoteItem
    {
        [JsonProperty("date")]
        public string Date { get; protected set; }
        [JsonProperty("time")]
        [JsonConverter(typeof(UnixDateTimeConverter))]
        public DateTimeOffset Time { get; protected set; }
        [JsonProperty("stamp")]
        public string Stamp { get; protected set; }
        [JsonProperty("value")]
        public int Value { get; protected set; }
        [JsonProperty("trend")]
        public int Trend { get; protected set; }
        [JsonProperty("post")]
        public Post Post { get; protected set; }
    }
}
