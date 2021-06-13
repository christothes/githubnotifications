using System.Text.Json.Serialization;

namespace GitHubNotifications.Models
{
    public class PullRequestIssue
    {
        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; set; }

        [JsonPropertyName("diff_url")]
        public string DiffUrl { get; set; }

        [JsonPropertyName("patch_url")]
        public string PatchUrl { get; set; }
    }
}
