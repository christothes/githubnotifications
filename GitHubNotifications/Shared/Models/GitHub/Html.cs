using System.Text.Json.Serialization;

namespace GitHubNotifications.Models
{
    public class Html
    {
        [JsonPropertyName("href")]
        public string Href { get; set; }
    }
}
