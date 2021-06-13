using System.Text.Json.Serialization;

namespace GitHubNotifications.Models
{
    public class PullRequestReviewEvent
    {
        [JsonPropertyName("action")]
        public string Action { get; set; }

        [JsonPropertyName("review")]
        public Review Review { get; set; }

        [JsonPropertyName("pull_request")]
        public PullRequest PullRequest { get; set; }

        [JsonPropertyName("repository")]
        public Repository Repository { get; set; }

        [JsonPropertyName("sender")]
        public User Sender { get; set; }
    }
}
