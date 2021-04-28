using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GitHubNotifications.Client
{
    public class Comment
    {
        public string Id { get; set; }
        public string User { get; set; }
        public string Uri { get; set; }
        public DateTime Created { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public string ReplyToUser { get; set; }
        public string ReplyToBody { get; set; }

        public Comment(string id, string user, string uri, DateTime created, string title, string body, string replyToUser, string replyToBody)
        {
            Id = id;
            User = user;
            Uri = uri;
            Created = created;
            Title = title;
            Body = body;
            ReplyToUser = replyToUser;
            ReplyToBody = replyToBody;
        }
    }
}
