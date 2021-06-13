using System;
using System.Text.Json.Serialization;

namespace GitHubNotifications.Models
{
    public class Review
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("node_id")]
        public string NodeId { get; set; }

        [JsonPropertyName("user")]
        public User User { get; set; }

        [JsonPropertyName("body")]
        public object Body { get; set; }

        [JsonPropertyName("commit_id")]
        public string CommitId { get; set; }

        [JsonPropertyName("submitted_at")]
        public DateTime SubmittedAt { get; set; }

        [JsonPropertyName("state")]
        public string State { get; set; }

        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; set; }

        [JsonPropertyName("pull_request_url")]
        public string PullRequestUrl { get; set; }

        [JsonPropertyName("author_association")]
        public string AuthorAssociation { get; set; }

        [JsonPropertyName("_links")]
        public Links Links { get; set; }
    }

}
