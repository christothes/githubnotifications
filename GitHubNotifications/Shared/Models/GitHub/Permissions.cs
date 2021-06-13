using System.Text.Json.Serialization;

namespace GitHubNotifications.Models
{
    public class Permissions
    {
        [JsonPropertyName("checks")]
        public string Checks { get; set; }

        [JsonPropertyName("contents")]
        public string Contents { get; set; }

        [JsonPropertyName("issues")]
        public string Issues { get; set; }

        [JsonPropertyName("metadata")]
        public string Metadata { get; set; }

        [JsonPropertyName("pull_requests")]
        public string PullRequests { get; set; }
    }
}
