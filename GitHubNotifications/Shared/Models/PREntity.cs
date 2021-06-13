using System;
using System.Linq;
using Azure;
using Azure.Data.Tables;

namespace GitHubNotifications.Models
{
    public class PREntity : ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
        public string Url { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public string Labels { get; set; }
        public string State { get; set; }
        public string RequestedReviewers { get; set; }

        public PREntity() { }

        public PREntity(PullRequestEvent evt)
        {
            PartitionKey = evt.Repository.Name;
            RowKey = evt.PullRequest.Number.ToString();
            Title = evt.PullRequest.Title;
            Url = evt.PullRequest.HtmlUrl;
            Author = evt.PullRequest.User.Login;
            State = evt.PullRequest.State;
            Labels = string.Join(";", evt.PullRequest.Labels.Select(l => l.Name));
            RequestedReviewers = string.Join(":", evt.PullRequest.RequestedReviewers?.Select(r => r.Login));
        }

        public PREntity(PullRequestReviewCommentEvent evt)
        {
            PartitionKey = evt.Repository.Name;
            RowKey = evt.PullRequest.Number.ToString();
            Title = evt.PullRequest.Title;
            Url = evt.PullRequest.HtmlUrl;
            Author = evt.PullRequest.User.Login;
            State = evt.PullRequest.State;
            Labels = string.Join(";", evt.PullRequest.Labels.Select(l => l.Name));
        }
    }
}
