namespace GitHubNotifications.Models
{
    public interface ICommentEvent
    {
        public Comment Comment { get; set; }
    }
}
