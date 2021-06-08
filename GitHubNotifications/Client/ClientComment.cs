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
        public string parentId { get; set; }
        public string parentAuthor { get; set; }
        public string prAuthor { get; set; }
        public string prNumber { get; set; }
        public Dictionary<string, ClientComment> replies { get; set; }
        public DateTime sortDate { get; set; }
        public DateTime updated { get; set; }
        public string labels { get; set; }
        public bool isEdited => updated != default && updated != created;
    }
}