using System.Text.Json.Serialization;

namespace GitHubNotifications.Models
{
    public class IssueEvent
    {
        [JsonPropertyName("action")]
        public string Action { get; set; }

        [JsonPropertyName("issue")]
        public Issue Issue { get; set; }

        [JsonPropertyName("repository")]
        public Repository Repository { get; set; }

        [JsonPropertyName("sender")]
        public User Sender { get; set; }
    }
}
