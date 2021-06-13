using System.Text.Json.Serialization;

namespace GitHubNotifications.Models
{
    public class CheckSuiteEvent
    {
        [JsonPropertyName("action")]
        public string Action { get; set; }

        [JsonPropertyName("check_suite")]
        public CheckSuite CheckSuite { get; set; }

        [JsonPropertyName("repository")]
        public Repository Repository { get; set; }

        [JsonPropertyName("organization")]
        public Organization Organization { get; set; }

        [JsonPropertyName("enterprise")]
        public Enterprise Enterprise { get; set; }

        [JsonPropertyName("sender")]
        public User Sender { get; set; }
    }
}
