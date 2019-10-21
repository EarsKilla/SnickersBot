using Newtonsoft.Json;

namespace SnickersBot.Models
{
    public class ProductItem
    {
        [JsonProperty("id")]
        public int Id { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("weight")]
        public string Weight { get; set; }
        [JsonProperty("image")]
        public string Image { get; set; }
    }
}
