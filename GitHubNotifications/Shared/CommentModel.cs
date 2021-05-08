using System;
using System.Text.Json.Serialization;

namespace GitHubNotifications.Shared
{
    public class CommentModel
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("author")]
        public string Author { get; set; }

        [JsonPropertyName("uri")]
        public string Uri { get; set; }

        [JsonPropertyName("created")]
        public DateTime Created { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("body")]
        public string Body { get; set; }

        [JsonPropertyName("replyToId")]

        public string ReplyToId { get; set; }

        [JsonPropertyName("replyToAuthor")]
        public string ReplyToAuthor { get; set; }

        [JsonPropertyName("replyToBody")]
        public string ReplyToBody { get; set; }

        public CommentModel(string id, string user, string uri, DateTime created, string title, string body, string replyToId, string replyToUser, string replyToBody)
        {
            Id = id;
            Author = user;
            Uri = uri;
            Created = created;
            Title = title;
            Body = body;
            ReplyToId = replyToId;
            ReplyToAuthor = replyToUser;
            ReplyToBody = replyToBody;
        }
    }
}
