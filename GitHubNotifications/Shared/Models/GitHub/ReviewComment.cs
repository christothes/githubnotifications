using System.Text.Json.Serialization;

namespace GitHubNotifications.Models
{
    public class ReviewComment
    {
        [JsonPropertyName("href")]
        public string Href { get; set; }
    }
}
