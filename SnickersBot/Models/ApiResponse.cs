using Newtonsoft.Json;

namespace SnickersBot.Models
{
    public class ApiResponse<T>
    {
        [JsonProperty("error")]
        public int? Error { get; protected set; }
        [JsonProperty("result")]
        public T Result { get; protected set; }
    }
}
