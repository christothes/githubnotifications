using System.Text.Json.Serialization;

namespace GitHubNotifications.Models
{
    public class LinksIssue
    {
        [JsonPropertyName("href")]
        public string Href { get; set; }
    }
}
