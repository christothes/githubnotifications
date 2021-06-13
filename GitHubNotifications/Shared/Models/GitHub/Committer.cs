using System.Text.Json.Serialization;

namespace GitHubNotifications.Models
{
    public class Committer
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }
    }
}
