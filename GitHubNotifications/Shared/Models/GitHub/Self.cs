using System.Text.Json.Serialization;

namespace GitHubNotifications.Models
{
    public class Self
    {
        [JsonPropertyName("href")]
        public string Href { get; set; }
    }
}
