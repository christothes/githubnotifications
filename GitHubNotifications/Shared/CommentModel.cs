using System;

namespace GitHubNotifications.Shared
{
    public class CommentModel
    {
        public string Id { get; set; }
        public string Author { get; set; }
        public string Uri { get; set; }
        public DateTime Created { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public string ReplyToAuthor { get; set; }
        public string ReplyToBody { get; set; }

        public CommentModel(string id, string user, string uri, DateTime created, string title, string body, string replyToUser, string replyToBody)
        {
            Id = id;
            Author = user;
            Uri = uri;
            Created = created;
            Title = title;
            Body = body;
            ReplyToAuthor = replyToUser;
            ReplyToBody = replyToBody;
        }
    }
}
