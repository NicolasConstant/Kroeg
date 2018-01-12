using System.Collections.Generic;
using Newtonsoft.Json;

namespace Kroeg.Mastodon
{
    public class Instance
    {
        [JsonProperty("uri")] public string uri { get; set; }
        [JsonProperty("title")] public string title { get; set; }
        [JsonProperty("description")] public string description { get; set; }
        [JsonProperty("email")] public string email { get; set; }
        [JsonProperty("version")] public string version { get; set; }
        [JsonProperty("urls")] public Dictionary<string, string> urls { get; } = new Dictionary<string, string>();
        [JsonProperty("stats")] public Dictionary<string, int> stats { get; } = new Dictionary<string, int>();
        [JsonProperty("thumbnail")] public string thumbnail { get; set; }
    }
}