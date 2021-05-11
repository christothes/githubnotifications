using System;
using Azure;
using Azure.Data.Tables;

namespace GitHubNotifications.Models
{

    public class UserOptions : ITableEntity
    {
        public string Login { get; set; }
        public bool OnlyMyPRs { get; set; }
        public string Labels { get; set; }
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}