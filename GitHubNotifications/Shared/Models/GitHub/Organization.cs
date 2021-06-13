using System.Text.Json.Serialization;

namespace GitHubNotifications.Models
{
    public class Organization
    {
        [JsonPropertyName("login")]
        public string Login { get; set; }

        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("node_id")]
        public string NodeId { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("repos_url")]
        public string ReposUrl { get; set; }

        [JsonPropertyName("events_url")]
        public string EventsUrl { get; set; }

        [JsonPropertyName("hooks_url")]
        public string HooksUrl { get; set; }

        [JsonPropertyName("issues_url")]
        public string IssuesUrl { get; set; }

        [JsonPropertyName("members_url")]
        public string MembersUrl { get; set; }

        [JsonPropertyName("public_members_url")]
        public string PublicMembersUrl { get; set; }

        [JsonPropertyName("avatar_url")]
        public string AvatarUrl { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }
    }
}
