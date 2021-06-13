using System.Text.Json.Serialization;

namespace GitHubNotifications.Models
{
    public class Commits
    {
        [JsonPropertyName("href")]
        public string Href { get; set; }
    }
}
