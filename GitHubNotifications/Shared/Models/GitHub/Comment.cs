using System;
using System.Text.Json.Serialization;

namespace GitHubNotifications.Models
{
    public class Comment
    {
        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("pull_request_review_id")]
        public int PullRequestReviewId { get; set; }

        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("node_id")]
        public string NodeId { get; set; }

        [JsonPropertyName("diff_hunk")]
        public string DiffHunk { get; set; }

        [JsonPropertyName("path")]
        public string Path { get; set; }

        [JsonPropertyName("position")]
        public int? Position { get; set; }

        [JsonPropertyName("original_position")]
        public int? OriginalPosition { get; set; }

        [JsonPropertyName("commit_id")]
        public string CommitId { get; set; }

        [JsonPropertyName("original_commit_id")]
        public string OriginalCommitId { get; set; }

        [JsonPropertyName("user")]
        public User User { get; set; }

        [JsonPropertyName("body")]
        public string Body { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; set; }

        [JsonPropertyName("pull_request_url")]
        public string PullRequestUrl { get; set; }

        [JsonPropertyName("author_association")]
        public string AuthorAssociation { get; set; }

        [JsonPropertyName("_links")]
        public Links Links { get; set; }

        [JsonPropertyName("start_line")]
        public object StartLine { get; set; }

        [JsonPropertyName("original_start_line")]
        public object OriginalStartLine { get; set; }

        [JsonPropertyName("start_side")]
        public object StartSide { get; set; }

        [JsonPropertyName("line")]
        public int? Line { get; set; }

        [JsonPropertyName("original_line")]
        public int? OriginalLine { get; set; }

        [JsonPropertyName("side")]
        public string Side { get; set; }

        [JsonPropertyName("in_reply_to_id")]
        public long InReplyToId { get; set; }
    }
}
