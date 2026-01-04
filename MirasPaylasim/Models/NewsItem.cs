namespace MirasPaylasim.Models
{
    public class NewsItem
    {
        public string Title { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string Link { get; set; } = string.Empty;
        public DateTime? PublishDate { get; set; }
        public string? ImageUrl { get; set; }
    }
}

