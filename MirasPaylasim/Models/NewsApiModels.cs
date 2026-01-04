using System.Text.Json.Serialization;

namespace MirasPaylasim.Models
{
    public class NewsApiResponse
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; }
        
        [JsonPropertyName("totalResults")]
        public int TotalResults { get; set; }
        
        [JsonPropertyName("articles")]
        public List<NewsApiArticle>? Articles { get; set; }
    }

    public class NewsApiArticle
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }
        
        [JsonPropertyName("description")]
        public string? Description { get; set; }
        
        [JsonPropertyName("url")]
        public string? Url { get; set; }
        
        [JsonPropertyName("urlToImage")]
        public string? UrlToImage { get; set; }
        
        [JsonPropertyName("publishedAt")]
        public DateTime? PublishedAt { get; set; }
        
        [JsonPropertyName("source")]
        public NewsApiSource? Source { get; set; }
    }

    public class NewsApiSource
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    // Gemini API Response Models
    public class GeminiResponse
    {
        [JsonPropertyName("candidates")]
        public List<GeminiCandidate>? Candidates { get; set; }
    }

    public class GeminiCandidate
    {
        [JsonPropertyName("content")]
        public GeminiContent? Content { get; set; }
    }

    public class GeminiContent
    {
        [JsonPropertyName("parts")]
        public List<GeminiPart>? Parts { get; set; }
    }

    public class GeminiPart
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }
}

