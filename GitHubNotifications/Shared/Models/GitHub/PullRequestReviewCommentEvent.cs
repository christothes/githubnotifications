using System.Text.Json.Serialization;

namespace GitHubNotifications.Models
{
    public class PullRequestReviewCommentEvent : ICommentEvent
    {
        [JsonPropertyName("action")]
        public string Action { get; set; }

        [JsonPropertyName("comment")]
        public Comment Comment { get; set; }

        [JsonPropertyName("pull_request")]
        public PullRequest PullRequest { get; set; }

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
