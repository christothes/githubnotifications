using System.Text.Json.Serialization;

namespace GitHubNotifications.Models
{
    public class ReviewComments
    {
        [JsonPropertyName("href")]
        public string Href { get; set; }
    }
}
