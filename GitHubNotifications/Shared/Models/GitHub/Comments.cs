using System.Text.Json.Serialization;

namespace GitHubNotifications.Models
{
    public class Comments
    {
        [JsonPropertyName("href")]
        public string Href { get; set; }
    }
}
