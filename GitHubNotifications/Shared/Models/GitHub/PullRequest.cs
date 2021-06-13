using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GitHubNotifications.Models
{
    public class PullRequest
    {
        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("node_id")]
        public string NodeId { get; set; }

        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; set; }

        [JsonPropertyName("diff_url")]
        public string DiffUrl { get; set; }

        [JsonPropertyName("patch_url")]
        public string PatchUrl { get; set; }

        [JsonPropertyName("issue_url")]
        public string IssueUrl { get; set; }

        [JsonPropertyName("number")]
        public int Number { get; set; }

        [JsonPropertyName("state")]
        public string State { get; set; }

        [JsonPropertyName("locked")]
        public bool Locked { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("user")]
        public User User { get; set; }

        [JsonPropertyName("body")]
        public string Body { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("closed_at")]
        public object ClosedAt { get; set; }

        [JsonPropertyName("merged_at")]
        public object MergedAt { get; set; }

        [JsonPropertyName("merge_commit_sha")]
        public string MergeCommitSha { get; set; }

        [JsonPropertyName("assignee")]
        public object Assignee { get; set; }

        [JsonPropertyName("assignees")]
        public List<object> Assignees { get; set; }

        [JsonPropertyName("requested_reviewers")]
        public List<User> RequestedReviewers { get; set; }

        [JsonPropertyName("requested_teams")]
        public List<object> RequestedTeams { get; set; }

        [JsonPropertyName("labels")]
        public List<Label> Labels { get; set; }

        [JsonPropertyName("milestone")]
        public object Milestone { get; set; }

        [JsonPropertyName("draft")]
        public bool Draft { get; set; }

        [JsonPropertyName("commits_url")]
        public string CommitsUrl { get; set; }

        [JsonPropertyName("review_comments_url")]
        public string ReviewCommentsUrl { get; set; }

        [JsonPropertyName("review_comment_url")]
        public string ReviewCommentUrl { get; set; }

        [JsonPropertyName("comments_url")]
        public string CommentsUrl { get; set; }

        [JsonPropertyName("statuses_url")]
        public string StatusesUrl { get; set; }

        [JsonPropertyName("head")]
        public Head Head { get; set; }

        [JsonPropertyName("base")]
        public Base Base { get; set; }

        [JsonPropertyName("_links")]
        public Links Links { get; set; }

        [JsonPropertyName("author_association")]
        public string AuthorAssociation { get; set; }

        [JsonPropertyName("auto_merge")]
        public object AutoMerge { get; set; }

        [JsonPropertyName("active_lock_reason")]
        public object ActiveLockReason { get; set; }

        [JsonPropertyName("merged")]
        public bool Merged { get; set; }

        [JsonPropertyName("mergeable")]
        public object Mergeable { get; set; }

        [JsonPropertyName("rebaseable")]
        public object Rebaseable { get; set; }

        [JsonPropertyName("mergeable_state")]
        public string MergeableState { get; set; }

        [JsonPropertyName("merged_by")]
        public object MergedBy { get; set; }

        [JsonPropertyName("comments")]
        public int Comments { get; set; }

        [JsonPropertyName("review_comments")]
        public int ReviewComments { get; set; }

        [JsonPropertyName("maintainer_can_modify")]
        public bool MaintainerCanModify { get; set; }

        [JsonPropertyName("commits")]
        public int Commits { get; set; }

        [JsonPropertyName("additions")]
        public int Additions { get; set; }

        [JsonPropertyName("deletions")]
        public int Deletions { get; set; }

        [JsonPropertyName("changed_files")]
        public int ChangedFiles { get; set; }
    }
}
