using System.Text.Json.Serialization;

namespace GitHubNotifications.Models
{
    public class Base
    {
        [JsonPropertyName("label")]
        public string Label { get; set; }

        [JsonPropertyName("ref")]
        public string Ref { get; set; }

        [JsonPropertyName("sha")]
        public string Sha { get; set; }

        [JsonPropertyName("user")]
        public User User { get; set; }

        [JsonPropertyName("repo")]
        public Repository Repo { get; set; }
    }
}
