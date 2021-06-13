using System;
using System.Linq;
using Azure;
using Azure.Data.Tables;

namespace GitHubNotifications.Models
{
    public class PRComment : ITableEntity
    {
        // PRauthor, tag
        public string PartitionKey { get; set; }
        // id
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
        public string Body { get; set; }
        public string Uri { get; set; }
        public DateTime Created { get; set; }
        public DateTime Updated { get; set; }
        public string ParentId { get; set; }
        public string ParentAuthor { get; set; }
        public string Author { get; set; }
        public string PrNumber { get; set; }
        public string PrTitle { get; set; }
        public string Labels { get; set; }

        public PRComment() { }

        public PRComment(PullRequestReviewCommentEvent pr)
        {
            PartitionKey = pr.PullRequest.User.Login;
            RowKey = pr.Comment.Id.ToString();
            Body = pr.Comment.Body;
            Uri = pr.Comment.HtmlUrl;
            Created = pr.Comment.CreatedAt;
            Updated = pr.Comment.UpdatedAt;
            ParentId = pr.Comment.InReplyToId.ToString();
            Author = pr.Comment.User.Login;
            PrNumber = pr.PullRequest.Number.ToString();
            PrTitle = pr.PullRequest.Title;
            Labels = string.Join(";", pr.PullRequest.Labels.Select(l => l.Name));
        }

        public PRComment(IssueCommentEvent i)
        {
            PartitionKey = i.Issue.User.Login;
            RowKey = i.Comment.Id.ToString();
            Body = i.Comment.Body;
            Uri = i.Comment.HtmlUrl;
            Created = i.Comment.CreatedAt;
            Updated = i.Comment.UpdatedAt;
            ParentId = i.Comment.InReplyToId.ToString();
            Author = i.Comment.User.Login;
            PrNumber = i.Issue.PullRequest != null ?
             i.Issue.PullRequest.Url.Substring(i.Issue.Url.LastIndexOf('/') + 1) :
             i.Issue.Number.ToString();
            PrTitle = i.Issue.Title;
            Labels = string.Join(";", i.Issue.Labels.Select(l => l.Name));
        }
    }
}
