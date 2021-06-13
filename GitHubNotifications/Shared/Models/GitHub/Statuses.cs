using System.Text.Json.Serialization;

namespace GitHubNotifications.Models
{
    public class Statuses
    {
        [JsonPropertyName("href")]
        public string Href { get; set; }
    }
}
