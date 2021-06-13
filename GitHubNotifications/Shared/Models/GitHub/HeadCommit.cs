using System;
using System.Text.Json.Serialization;

namespace GitHubNotifications.Models
{
    public class HeadCommit
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("tree_id")]
        public string TreeId { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonPropertyName("author")]
        public Author Author { get; set; }

        [JsonPropertyName("committer")]
        public Committer Committer { get; set; }
    }
}
