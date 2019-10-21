using Newtonsoft.Json;

namespace SnickersBot.Models.Quote
{
    public class Post
    {
        [JsonProperty("user")]
        public string User { get; protected set; }
        [JsonProperty("message")]
        public string Message { get; protected set; }
    }
}
