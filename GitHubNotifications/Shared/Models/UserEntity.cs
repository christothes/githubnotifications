using System;
using Azure;
using Azure.Data.Tables;

namespace GitHubNotifications.Models
{
    public class UserEntity : ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
        public bool MyPrsOnly { get; set; }
        public string Labels { get; set; }
    }
}
