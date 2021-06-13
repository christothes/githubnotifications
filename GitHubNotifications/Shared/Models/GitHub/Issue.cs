using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GitHubNotifications.Models
{
    public class Issue
    {
        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("repository_url")]
        public string RepositoryUrl { get; set; }

        [JsonPropertyName("labels_url")]
        public string LabelsUrl { get; set; }

        [JsonPropertyName("comments_url")]
        public string CommentsUrl { get; set; }

        [JsonPropertyName("events_url")]
        public string EventsUrl { get; set; }

        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; set; }

        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("node_id")]
        public string NodeId { get; set; }

        [JsonPropertyName("number")]
        public int Number { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("user")]
        public User User { get; set; }

        [JsonPropertyName("labels")]
        public List<Label> Labels { get; set; }

        [JsonPropertyName("state")]
        public string State { get; set; }

        [JsonPropertyName("locked")]
        public bool Locked { get; set; }

        [JsonPropertyName("assignee")]
        public User Assignee { get; set; }

        [JsonPropertyName("assignees")]
        public List<User> Assignees { get; set; }

        [JsonPropertyName("milestone")]
        public object Milestone { get; set; }

        [JsonPropertyName("comments")]
        public int Comments { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("closed_at")]
        public object ClosedAt { get; set; }

        [JsonPropertyName("author_association")]
        public string AuthorAssociation { get; set; }

        [JsonPropertyName("active_lock_reason")]
        public object ActiveLockReason { get; set; }

        [JsonPropertyName("pull_request")]
        public PullRequestIssue PullRequest { get; set; }

        [JsonPropertyName("body")]
        public string Body { get; set; }

        [JsonPropertyName("performed_via_github_app")]
        public object PerformedViaGithubApp { get; set; }
    }
}
