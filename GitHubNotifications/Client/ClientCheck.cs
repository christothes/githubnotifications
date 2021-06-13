using System;
using System.Collections.Generic;

namespace GitHubNotifications.Client
{
    public class ClientCheck
    {
        public DateTime updated { get; set; }
        public string id { get; set; }
        public string conclusion { get; set; }
        public string title { get; set; }
        public string url { get; set; }
        public string author { get; set; }
    }
}