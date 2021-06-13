using System.Text.Json.Serialization;

namespace GitHubNotifications.Shared
{
    public class GitHubEventEnvelope
    {
        [JsonPropertyName("X-GitHub-Event")]
        public string[] XGitHubEvent { get; set; }
    }
}