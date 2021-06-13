using System.Text.Json.Serialization;

namespace GitHubNotifications.Models
{
    public class PullRequestEvent
    {
        [JsonPropertyName("action")]
        public string Action { get; set; }

        [JsonPropertyName("number")]
        public int Number { get; set; }

        [JsonPropertyName("pull_request")]
        public PullRequest PullRequest { get; set; }

        [JsonPropertyName("before")]
        public string Before { get; set; }

        [JsonPropertyName("after")]
        public string After { get; set; }

        [JsonPropertyName("repository")]
        public Repository Repository { get; set; }

        [JsonPropertyName("organization")]
        public Organization Organization { get; set; }

        [JsonPropertyName("enterprise")]
        public Enterprise Enterprise { get; set; }

        [JsonPropertyName("sender")]
        public User Sender { get; set; }
    }
}
