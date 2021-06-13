using System.Text.Json.Serialization;

namespace GitHubNotifications.Models
{
    public class Links
    {
        [JsonPropertyName("self")]
        public Self Self { get; set; }

        [JsonPropertyName("html")]
        public Html Html { get; set; }

        [JsonPropertyName("issue")]
        public LinksIssue Issue { get; set; }

        [JsonPropertyName("comments")]
        public Comments Comments { get; set; }

        [JsonPropertyName("review_comments")]
        public ReviewComments ReviewComments { get; set; }

        [JsonPropertyName("review_comment")]
        public ReviewComment ReviewComment { get; set; }

        [JsonPropertyName("commits")]
        public Commits Commits { get; set; }

        [JsonPropertyName("statuses")]
        public Statuses Statuses { get; set; }
    }
}
