namespace Sntndr.Api.Models
{
    public sealed class Story
    {
        public string Title { get; set; } = default!;
        public string Uri { get; set; } = default!;
        public string PostedBy { get; set; } = default!;
        public DateTime Time { get; set; }
        public int Score { get; set; }
        public int CommentCount { get; set; }
    }
}
