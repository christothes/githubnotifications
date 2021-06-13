using System.Text.Json.Serialization;

namespace GitHubNotifications.Models
{

    public class Label
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("node_id")]
        public string NodeId { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("color")]
        public string Color { get; set; }

        [JsonPropertyName("default")]
        public bool Default { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }
    }
}
