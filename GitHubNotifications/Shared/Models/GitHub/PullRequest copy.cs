using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GitHubNotifications.Models
{
    public class PullRequestCheck
    {
        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("number")]
        public int Number { get; set; }
    
        [JsonPropertyName("head")]
        public Head Head { get; set; }

        [JsonPropertyName("base")]
        public Base Base { get; set; }
    }
}
