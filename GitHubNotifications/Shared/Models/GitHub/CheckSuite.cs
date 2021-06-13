using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GitHubNotifications.Models
{
    public class CheckSuite
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("node_id")]
        public string NodeId { get; set; }

        [JsonPropertyName("head_branch")]
        public object HeadBranch { get; set; }

        [JsonPropertyName("head_sha")]
        public string HeadSha { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("conclusion")]
        public string Conclusion { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("before")]
        public object Before { get; set; }

        [JsonPropertyName("after")]
        public object After { get; set; }

        [JsonPropertyName("pull_requests")]
        public List<PullRequestCheck> PullRequests { get; set; }

        [JsonPropertyName("app")]
        public App App { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("latest_check_runs_count")]
        public int LatestCheckRunsCount { get; set; }

        [JsonPropertyName("check_runs_url")]
        public string CheckRunsUrl { get; set; }

        [JsonPropertyName("head_commit")]
        public HeadCommit HeadCommit { get; set; }
    }
}
