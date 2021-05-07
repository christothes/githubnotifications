using System;

namespace GitHubNotifications.Client
{
    public class Comment
    {
        public string id { get; set; }
        public string author { get; set; }
        public string uri { get; set; }
        public DateTime created { get; set; }
        public string title { get; set; }
        public string body { get; set; }
        public string replyToAuthor { get; set; }
        public string replyToBody { get; set; }
    }
}