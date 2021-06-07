using System.Collections.Generic;

namespace GitHubNotifications.Server{
    public class WorkerMessage{
        public string MessageType { get; set; }
        public Dictionary<string, string> Properties { get; set; }
    }
}