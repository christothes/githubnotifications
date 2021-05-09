using System;
using System.Collections.Generic;

namespace GitHubNotifications.Client
{
    public class ClientComment
    {
        public string id { get; set; }
        public string author { get; set; }
        public string uri { get; set; }
        public DateTime created { get; set; }
        public string title { get; set; }
        public string body { get; set; }
        public string replyToId { get; set; }
        public ClientComment parent { get; set; }
        public List<ClientComment> replies { get; set; }
        public DateTime sortDate { get; set; }
    }
}