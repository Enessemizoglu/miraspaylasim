using System.Diagnostics;
using System.ServiceModel.Syndication;
using System.Xml;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MirasPaylasim.Models;

namespace MirasPaylasim.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _configuration;
    private readonly Services.GeminiNewsFilterService _geminiFilter;

    public HomeController(
        ILogger<HomeController> logger, 
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        IConfiguration configuration,
        Services.GeminiNewsFilterService geminiFilter)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _configuration = configuration;
        _geminiFilter = geminiFilter;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            // Cache'den haberleri kontrol et (6 saat cache - performans için)
            var cacheKey = "inheritance_news";
            
            if (!_cache.TryGetValue(cacheKey, out List<NewsItem> news))
            {
                news = await GetInheritanceNews();
                
                // 6 saat cache'le (daha uzun cache = daha hızlı sayfa yükleme)
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(6),
                    SlidingExpiration = TimeSpan.FromHours(3)
                };
                
                _cache.Set(cacheKey, news, cacheOptions);
            }
            
            ViewBag.News = news;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Haberler yüklenirken hata oluştu");
            ViewBag.News = GetFallbackNews();
        }
        
        return View();
    }
    
    private async Task<List<NewsItem>> GetInheritanceNews()
    {
        var news = new List<NewsItem>();
        
        // 1. ÖNCE Gemini Search dene (en doğru sonuçlar)
        _logger.LogInformation("Gemini Search ile haber çekiliyor...");
        try
        {
            var geminiNews = await _geminiFilter.GetNewsFromGeminiSearch(3);
            if (geminiNews.Any())
            {
                news = geminiNews.Take(3).ToList();
                _logger.LogInformation("Gemini Search'ten {Count} haber alındı", news.Count);
                
                // Eğer yeterli değilse yedek haberlerle tamamla
                if (news.Count < 3)
                {
                    var fallbackNews = GetFallbackNews();
                    var needed = 3 - news.Count;
                    news.AddRange(fallbackNews.Take(needed));
                    _logger.LogInformation("Yedek haberlerle {Count} haber eklendi, toplam {Total} haber", needed, news.Count);
                }
                
                return news;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Gemini Search hatası, NewsAPI deneniyor: {Error}", ex.Message);
        }
        
        // 2. Gemini Search başarısız olursa, NewsAPI'yi ÇOK KATI filtreleme ile kullan
        _logger.LogInformation("Gemini Search başarısız, NewsAPI çok katı filtreleme ile deneniyor...");
        var newsApiEnabled = _configuration.GetValue<bool>("NewsApi:Enabled", false);
        var newsApiKey = _configuration["NewsApi:ApiKey"];
        
        if (newsApiEnabled && !string.IsNullOrEmpty(newsApiKey) && newsApiKey != "YOUR_API_KEY_HERE")
        {
            try
            {
                var newsApiNews = await GetNewsFromNewsAPI(newsApiKey);
                if (newsApiNews.Any())
                {
                    // ÇOK KATI filtreleme - sadece kesin miras hukuku haberleri
                    news = newsApiNews
                        .Where(n => IsNewsRelevantStrict(n))
                        .Take(3)
                        .ToList();
                    
                    _logger.LogInformation("NewsAPI'den {Count} uygun haber bulundu (katı filtreleme)", news.Count);
                    
                    // Eğer yeterli değilse yedek haberlerle tamamla
                    if (news.Count < 3)
                    {
                        var fallbackNews = GetFallbackNews();
                        var needed = 3 - news.Count;
                        news.AddRange(fallbackNews.Take(needed));
                        _logger.LogInformation("Yedek haberlerle {Count} haber eklendi, toplam {Total} haber", needed, news.Count);
                    }
                    
                    return news;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "NewsAPI hatası: {Error}", ex.Message);
            }
        }
        
        // 3. Son çare: Yedek haberler (her zaman 3 haber)
        _logger.LogInformation("Tüm kaynaklar başarısız, yedek haberler kullanılıyor");
        return GetFallbackNews();
    }
    
    // Esnek filtreleme - "miras paylaşımı" gibi kelimeler geçiyorsa kabul et
    private bool IsNewsRelevantStrict(NewsItem news)
    {
        if (news == null) return false;
        
        var text = $"{news.Title} {news.Summary}".ToLowerInvariant();
        
        // ÖNCE: False positive kontrolü - bunlar varsa KESINLIKLE reddet
        var falsePositives = new[] { 
            "kültürel miras", "tarihi miras", "doğal miras", 
            "unesco", "balkan", "osmanlı eserleri", "kütüphane",
            "turizm", "korkuluk", "müze", "sanat", "kültür",
            "barbaros", "hatay", "makedonya", "izmir", "urla",
            "deprem", "dünya mirası", "arkeolojik", "tarih"
        };
        
        // Eğer false positive varsa, kesinlikle reddet
        if (falsePositives.Any(fp => text.Contains(fp)))
        {
            return false; // KESINLIKLE reddet
        }
        
        // SONRA: Miras hukuku ile ilgili keyword'ler - bunlardan biri geçmeli
        // "miras hukuku" kelimesi ŞART DEĞİL, diğer kelimeler de yeterli
        var relevantKeywords = new[] { 
            "miras paylaşımı", "miras paylaşım", "veraset", "tereke", 
            "vasiyet", "saklı pay", "miras davası", "yargıtay miras",
            "miras sözleşmesi", "intikal işlemleri", "mirasçılık",
            "zümre", "halefiyet", "medeni kanun miras", "mirasçı",
            "miras payı", "tenkis", "veraset ilamı", "miras hukuku",
            "miras payı hesaplama", "miras paylaşımı", "miras bırakan",
            "mirasçı hakları", "miras reddi", "miras sebebiyle"
        };
        
        // En az 1 relevant keyword geçmeli
        var hasRelevantKeyword = relevantKeywords.Any(keyword => text.Contains(keyword));
        
        // Eğer sadece "miras" kelimesi geçiyorsa ama false positive yoksa, şüpheli
        // Ama "miras paylaşımı" gibi kelimeler geçiyorsa kesinlikle kabul et
        if (hasRelevantKeyword)
        {
            return true;
        }
        
        // Sadece "miras" kelimesi geçiyorsa ama false positive yoksa, kabul et
        // (Çünkü "miras paylaşımı" gibi kelimeler zaten yukarıda yakalanıyor)
        if (text.Contains("miras") && !text.Contains("kültürel") && !text.Contains("tarihi"))
        {
            return true;
        }
        
        return false;
    }
    
    // Haberin miras hukuku ile ilgili olup olmadığını kontrol et
    private bool IsNewsRelevant(NewsItem news)
    {
        if (news == null) return false;
        
        var text = $"{news.Title} {news.Summary}".ToLowerInvariant();
        
        // Güçlü keyword'ler
        var strongKeywords = new[] { 
            "miras hukuku", "miras paylaşımı", "veraset", "tereke", 
            "vasiyet", "saklı pay", "miras davası", "yargıtay miras",
            "miras sözleşmesi", "intikal işlemleri", "mirasçılık",
            "zümre", "halefiyet", "medeni kanun miras"
        };
        
        var hasStrongKeyword = strongKeywords.Any(keyword => text.Contains(keyword));
        
        if (!hasStrongKeyword) return false;
        
        // False positive kontrolü
        var falsePositives = new[] { 
            "kültürel miras", "tarihi miras", "doğal miras", 
            "unesco", "balkan", "osmanlı eserleri", "kütüphane",
            "turizm", "korkuluk", "müze", "sanat"
        };
        
        var hasFalsePositive = falsePositives.Any(fp => text.Contains(fp));
        
        // Eğer false positive varsa ama güçlü keyword de varsa, yine de kabul et
        return !hasFalsePositive || strongKeywords.Any(kw => text.Contains(kw) && kw != "miras");
    }
    
    private async Task<List<NewsItem>> GetNewsFromNewsAPI(string apiKey)
    {
        var news = new List<NewsItem>();
        
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(15);
            client.DefaultRequestHeaders.Add("User-Agent", "MirasPaylasim/1.0");
            
            // ÖNCE: Top-headlines endpoint'ini dene (production'da çalışır)
            // Hukuk odaklı haberler için query parametresi ekle
            var topHeadlinesUrl = $"https://newsapi.org/v2/top-headlines?country=tr&pageSize=50&apiKey={apiKey}";
            
            _logger.LogInformation("Top-headlines endpoint deneniyor (production'da çalışır)...");
            
            var response = await client.GetAsync(topHeadlinesUrl);
            var json = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Top-headlines yanıtı alındı: {Length} karakter", json.Length);
                
                var apiResponse = JsonSerializer.Deserialize<NewsApiResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                
                if (apiResponse?.Status == "ok" && apiResponse.Articles != null && apiResponse.Articles.Any())
                {
                    news = await ProcessArticles(apiResponse.Articles, "top-headlines");
                    if (news.Count >= 3)
                    {
                        _logger.LogInformation("Top-headlines'den {Count} haber bulundu", news.Count);
                        return news;
                    }
                }
            }
            
            // Everything endpoint'i dene (basitleştirilmiş query ile)
            _logger.LogInformation("Everything endpoint deneniyor (basitleştirilmiş query ile)...");
            
            // Basitleştirilmiş query - karmaşık AND/OR/NOT yerine basit OR
            var simpleQuery = "miras OR veraset OR tereke OR vasiyet OR mirasçı";
            var everythingUrl = $"https://newsapi.org/v2/everything?q={Uri.EscapeDataString(simpleQuery)}&language=tr&sortBy=publishedAt&pageSize=50&apiKey={apiKey}";
            
            response = await client.GetAsync(everythingUrl);
            json = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
                    {
                _logger.LogInformation("Everything yanıtı alındı: {Length} karakter", json.Length);
                
                var apiResponse = JsonSerializer.Deserialize<NewsApiResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                                
                if (apiResponse?.Status == "ok" && apiResponse.Articles != null && apiResponse.Articles.Any())
                {
                    var everythingNews = await ProcessArticles(apiResponse.Articles, "everything");
                                
                    // Everything'den gelen haberleri mevcut haberlerle birleştir (duplicate kontrolü ile)
                    var existingUrls = news.Select(n => n.Link).ToList();
                    var newNews = everythingNews.Where(n => !existingUrls.Contains(n.Link)).ToList();
                    
                    news.AddRange(newNews);
                    news = news.Take(3).ToList();
                        
                    _logger.LogInformation("Everything'den {Count} yeni haber eklendi, toplam {Total} haber", newNews.Count, news.Count);
                }
            }
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("NewsAPI isteği zaman aşımına uğradı");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "NewsAPI çağrısında beklenmeyen hata");
        }
        
        return news;
    }
    
    private async Task<List<NewsItem>> ProcessArticles(List<NewsApiArticle> articles, string source)
    {
        var allArticles = articles
            .Where(article => 
                !string.IsNullOrEmpty(article.Title) &&
                !string.IsNullOrEmpty(article.Description))
            .ToList();

        if (!allArticles.Any())
        {
            return new List<NewsItem>();
        }

        // Önce Gemini ile akıllı filtreleme dene
        try
        {
            var geminiFiltered = await _geminiFilter.FilterInheritanceNews(allArticles, 3);
            
            if (geminiFiltered.Count >= 3)
            {
                _logger.LogInformation("{Source}: Gemini API {Count} miras hukuku haberi seçti", source, geminiFiltered.Count);
                return geminiFiltered.Select(art => new NewsItem
                {
                    Title = CleanText(art.Title ?? ""),
                    Summary = CleanText(art.Description ?? ""),
                    Link = art.Url ?? "#",
                    PublishDate = art.PublishedAt,
                    ImageUrl = art.UrlToImage
                }).ToList();
            }
            
            // Gemini yeterli haber bulamadıysa, eski yöntemle tamamla
            var selectedNews = geminiFiltered.Select(art => new NewsItem
            {
                Title = CleanText(art.Title ?? ""),
                Summary = CleanText(art.Description ?? ""),
                Link = art.Url ?? "#",
                PublishDate = art.PublishedAt,
                ImageUrl = art.UrlToImage
            }).ToList();

            var remainingArticles = allArticles
                .Where(article => !geminiFiltered.Any(gf => gf.Url == article.Url))
                .ToList();

            // Eksik varsa Genel Hukuk ile tamamla
            if (selectedNews.Count < 3)
            {
                var legalKeywords = new[] { 
                    "hukuk", "dava", "mahkeme", "yargıtay", 
                    "avukat", "kanun", "yargı", "hukuki", 
                    "ceza", "medeni kanun", "borçlar kanunu",
                    "icra", "iflas", "aile hukuku"
                };
                
                var legalArticles = remainingArticles
                    .Where(article => 
                    {
                        var text = $"{article.Title} {article.Description}".ToLowerInvariant();
                        return legalKeywords.Any(keyword => text.Contains(keyword));
                    })
                    .Take(3 - selectedNews.Count)
                    .ToList();

                selectedNews.AddRange(legalArticles.Select(art => new NewsItem {
                    Title = CleanText(art.Title ?? ""),
                    Summary = CleanText(art.Description ?? ""),
                    Link = art.Url ?? "#",
                    PublishDate = art.PublishedAt,
                    ImageUrl = art.UrlToImage
                }));
                
                _logger.LogInformation("{Source}: Genel hukuk haberleri: {Count}", source, legalArticles.Count);
            }

            // Hala eksik varsa Genel Haberler
            if (selectedNews.Count < 3)
            {
                var takenUrls = selectedNews.Select(n => n.Link).ToList();
                var remaining = remainingArticles
                    .Where(article => !takenUrls.Contains(article.Url ?? ""))
                    .Take(3 - selectedNews.Count)
                    .ToList();

                selectedNews.AddRange(remaining.Select(art => new NewsItem {
                    Title = CleanText(art.Title ?? ""),
                    Summary = CleanText(art.Description ?? ""),
                    Link = art.Url ?? "#",
                    PublishDate = art.PublishedAt,
                    ImageUrl = art.UrlToImage
                }));
                
                _logger.LogInformation("{Source}: Genel haberler: {Count}", source, remaining.Count);
            }

            return selectedNews;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Source}: Gemini hatası, eski filtrelemeye dönülüyor", source);
            // Hata durumunda eski yöntemi kullan
            return ProcessArticlesFallback(allArticles, source);
        }
    }
    
    private List<NewsItem> ProcessArticlesFallback(List<NewsApiArticle> allArticles, string source)
    {
        var selectedNews = new List<NewsItem>();

        // Genişletilmiş miras hukuku anahtar kelimeleri
        var inheritanceKeywords = new[] { 
            "miras", "veraset", "tereke", "vasiyet", "mirasçı", 
            "saklı pay", "miras paylaşımı", "miras hukuku", 
            "miras davası", "yargıtay miras", "miras sözleşmesi",
            "intikal", "halefiyet", "zümre", "miras payı",
            "miras bırakan", "temsilen", "mirasçılık"
        };
        
        // False positive'ler - bunlar kesinlikle miras hukuku değil
        var falsePositives = new[] { 
            "kültürel miras", "tarihi miras", "doğal miras", 
            "unesco", "balkan", "osmanlı eserleri", "kütüphane",
            "turizm", "korkuluk", "müze", "sanat", "kültür",
            "barbaros", "hatay", "makedonya", "izmir", "urla",
            "deprem", "dünya mirası", "arkeolojik", "tarih"
        };
        
        // 1. Miras Hukuku (Öncelikli) - false positive kontrolü ile
        var inheritanceArticles = allArticles
            .Where(article => 
            {
                var text = $"{article.Title} {article.Description}".ToLowerInvariant();
                
                // Önce false positive kontrolü
                if (falsePositives.Any(fp => text.Contains(fp)))
                {
                    return false; // Kesinlikle reddet
                }
                
                // Sonra keyword kontrolü
                return inheritanceKeywords.Any(keyword => text.Contains(keyword));
            })
            .Take(3)
            .ToList();

        selectedNews.AddRange(inheritanceArticles.Select(art => new NewsItem {
            Title = CleanText(art.Title ?? ""),
            Summary = CleanText(art.Description ?? ""),
            Link = art.Url ?? "#",
            PublishDate = art.PublishedAt,
            ImageUrl = art.UrlToImage
        }));

        _logger.LogInformation("{Source}: Miras hukuku haberleri: {Count}", source, inheritanceArticles.Count);

        // 2. Eksik varsa Genel Hukuk
        if (selectedNews.Count < 3)
        {
            var legalKeywords = new[] { 
                "hukuk", "dava", "mahkeme", "yargıtay", 
                "avukat", "kanun", "yargı", "hukuki", 
                "ceza", "medeni kanun", "borçlar kanunu",
                "icra", "iflas", "aile hukuku"
            };
            
            var legalArticles = allArticles
                .Where(article => !inheritanceArticles.Any(ia => ia.Url == article.Url))
                .Where(article => 
                {
                    var text = $"{article.Title} {article.Description}".ToLowerInvariant();
                    return legalKeywords.Any(keyword => text.Contains(keyword));
                })
                .Take(3 - selectedNews.Count)
                .ToList();

            selectedNews.AddRange(legalArticles.Select(art => new NewsItem {
                Title = CleanText(art.Title ?? ""),
                Summary = CleanText(art.Description ?? ""),
                Link = art.Url ?? "#",
                PublishDate = art.PublishedAt,
                ImageUrl = art.UrlToImage
            }));
            
            _logger.LogInformation("{Source}: Genel hukuk haberleri: {Count}", source, legalArticles.Count);
        }

        // 3. Hala eksik varsa Genel Haberler
        if (selectedNews.Count < 3)
        {
            var takenUrls = selectedNews.Select(n => n.Link).ToList();
            var remainingArticles = allArticles
                .Where(article => !takenUrls.Contains(article.Url ?? ""))
                .Take(3 - selectedNews.Count)
                .ToList();

            selectedNews.AddRange(remainingArticles.Select(art => new NewsItem {
                Title = CleanText(art.Title ?? ""),
                Summary = CleanText(art.Description ?? ""),
                Link = art.Url ?? "#",
                PublishDate = art.PublishedAt,
                ImageUrl = art.UrlToImage
            }));
            
            _logger.LogInformation("{Source}: Genel haberler: {Count}", source, remainingArticles.Count);
        }

        return selectedNews;
    }
    
    private async Task<List<NewsItem>> TryTopHeadlinesEndpoint(HttpClient client, string apiKey)
    {
        var news = new List<NewsItem>();
        
        try
        {
            // Top headlines endpoint - kategori bazlı (business veya general)
            var url = $"https://newsapi.org/v2/top-headlines?country=tr&category=general&pageSize=50&apiKey={apiKey}";
            
            _logger.LogInformation("Top-headlines endpoint deneniyor...");
            
            var response = await client.GetAsync(url);
            var json = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                var apiResponse = JsonSerializer.Deserialize<NewsApiResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                
                if (apiResponse?.Status == "ok" && apiResponse.Articles != null)
                {
                    _logger.LogInformation("Top-headlines'den {Count} haber alındı, akıllı filtreleme uygulanıyor...", apiResponse.Articles.Count);
                    
                    var allArticles = apiResponse.Articles
                        .Where(article => 
                            !string.IsNullOrEmpty(article.Title) &&
                            !string.IsNullOrEmpty(article.Description))
                        .ToList();

                    var selectedNews = new List<NewsItem>();

                    // 1. Miras Hukuku
                    var inheritanceArticles = allArticles
                        .Where(article => 
                        {
                            var text = $"{article.Title} {article.Description}".ToLowerInvariant();
                            return text.Contains("miras") || text.Contains("veraset") || text.Contains("tereke") || 
                                   text.Contains("vasiyet") || text.Contains("mirasçı") || text.Contains("saklı pay");
                        })
                        .Take(3)
                        .ToList();

                    selectedNews.AddRange(inheritanceArticles.Select(art => new NewsItem {
                        Title = CleanText(art.Title ?? ""),
                        Summary = CleanText(art.Description ?? ""),
                        Link = art.Url ?? "#",
                        PublishDate = art.PublishedAt,
                        ImageUrl = art.UrlToImage
                    }));

                    // 2. Eksik varsa Genel Hukuk
                    if (selectedNews.Count < 3)
                    {
                        var legalArticles = allArticles
                            .Where(article => !inheritanceArticles.Any(ia => ia.Url == article.Url))
                            .Where(article => 
                            {
                                var text = $"{article.Title} {article.Description}".ToLowerInvariant();
                                return text.Contains("hukuk") || text.Contains("dava") || text.Contains("mahkeme") ||
                                       text.Contains("yargıtay") || text.Contains("avukat") || text.Contains("kanun");
                            })
                            .Take(3 - selectedNews.Count)
                            .ToList();

                        selectedNews.AddRange(legalArticles.Select(art => new NewsItem {
                            Title = CleanText(art.Title ?? ""),
                            Summary = CleanText(art.Description ?? ""),
                            Link = art.Url ?? "#",
                            PublishDate = art.PublishedAt,
                            ImageUrl = art.UrlToImage
                        }));
                    }

                    // 3. Hala eksik varsa Genel Haberler
                    if (selectedNews.Count < 3)
                    {
                        var takenUrls = selectedNews.Select(n => n.Link).ToList();
                        var remainingArticles = allArticles
                            .Where(article => !takenUrls.Contains(article.Url ?? ""))
                            .Take(3 - selectedNews.Count)
                            .ToList();

                        selectedNews.AddRange(remainingArticles.Select(art => new NewsItem {
                            Title = CleanText(art.Title ?? ""),
                            Summary = CleanText(art.Description ?? ""),
                            Link = art.Url ?? "#",
                            PublishDate = art.PublishedAt,
                            ImageUrl = art.UrlToImage
                        }));
                    }

                    news = selectedNews;
                    _logger.LogInformation("Top-headlines'den toplam {Count} haber gösteriliyor", news.Count);
                }
            }
            else
            {
                _logger.LogWarning("Top-headlines HTTP hatası: {StatusCode} - {Content}", 
                    response.StatusCode, json.Substring(0, Math.Min(300, json.Length)));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Top-headlines endpoint çağrısında hata");
        }
        
        return news;
    }
    
    private bool IsRelevantToInheritanceLaw(string title, string description, string[] keywords)
    {
        if (string.IsNullOrEmpty(title) && string.IsNullOrEmpty(description))
            return false;
        
        var combinedText = $"{title} {description}".ToLowerInvariant();
        
        // newsprd.md'ye göre geçerli anahtar kelimeler (en güçlü versiyon)
        var validKeywords = new[] { 
            "miras hukuku", "miras", "miras paylaşımı", "veraset", 
            "tereke", "intikal işlemleri", "saklı pay", "vasiyet",
            "miras davaları", "yargıtay miras kararı", "miras sözleşmesi",
            "mirasçı", "mirasçılık", "zümre", "medeni kanun", 
            "miras payı", "miras bırakan", "halefiyet", "temsilen"
        };
        
        // Geçerli anahtar kelimelerden en az biri geçmeli
        var hasValidKeyword = validKeywords.Any(keyword => 
            combinedText.Contains(keyword.ToLowerInvariant()));
        
        if (!hasValidKeyword)
            return false;
        
        // İlgisiz kelimeleri filtrele (newsprd.md'ye göre)
        var irrelevantKeywords = new[] { 
            "spor", "magazin", "dizi", "film", "kaza", "siyaset", 
            "borsa", "kripto", "futbol", "müzik", "sanat", 
            "teknoloji", "oyun", "yemek", "seyahat", 
            "kültürel miras", "tarihi miras", "doğal miras", 
            "unesco", "yarışma", "ölüm ilanı", "ekonomi"
        };
        
        // İlgisiz kelimelerden herhangi biri geçiyorsa filtrele
        var hasIrrelevantKeyword = irrelevantKeywords.Any(keyword => 
            combinedText.Contains(keyword));
        
        if (hasIrrelevantKeyword)
        {
            // Ama eğer "miras hukuku", "veraset", "tereke", "vasiyet" gibi güçlü anahtar kelimeler varsa kabul et
            var strongKeywords = new[] { 
                "miras hukuku", "veraset", "tereke", "vasiyet",
                "miras davaları", "yargıtay miras kararı", 
                "saklı pay", "intikal işlemleri", "miras sözleşmesi"
            };
            var hasStrongKeyword = strongKeywords.Any(keyword => 
                combinedText.Contains(keyword));
            
            // Güçlü anahtar kelime yoksa filtrele
            if (!hasStrongKeyword)
                return false;
        }
        
        return true;
    }
    
    private string CleanText(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        
        // HTML etiketlerini temizle
        text = System.Text.RegularExpressions.Regex.Replace(text, "<.*?>", "");
        text = System.Net.WebUtility.HtmlDecode(text);
        
        // Fazla boşlukları temizle
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ");
        text = text.Trim();
        
        // Maksimum uzunluk
        if (text.Length > 200)
        {
            text = text.Substring(0, 197) + "...";
        }
        
        return text;
    }
    
    private List<NewsItem> GetFallbackNews()
    {
        return new List<NewsItem>
        {
            new NewsItem
            {
                Title = "Miras Hukukunda Yeni Düzenlemeler",
                Summary = "Türk Medeni Kanunu'nda miras paylaşımı ile ilgili yeni düzenlemeler yürürlüğe girdi.",
                Link = "#",
                PublishDate = DateTime.Now.AddDays(-2)
            },
            new NewsItem
            {
                Title = "Zümre Sistemi ve Mirasçılık",
                Summary = "Miras hukukunda zümre sistemi nasıl çalışır? Hangi zümreler mirasçı olabilir?",
                Link = "#",
                PublishDate = DateTime.Now.AddDays(-5)
            },
            new NewsItem
            {
                Title = "Eşin Miras Payı Hesaplama",
                Summary = "Eşin miras payı, diğer mirasçıların durumuna göre nasıl belirlenir?",
                Link = "#",
                PublishDate = DateTime.Now.AddDays(-7)
            }
        };
    }

    public IActionResult Privacy()
    {
        return View();
    }
    
    // Debug: Cache'i temizle ve haberleri yeniden çek
    public async Task<IActionResult> RefreshNews()
    {
        _cache.Remove("inheritance_news");
        _logger.LogInformation("Haber cache'i temizlendi");
        return RedirectToAction("Index");
    }
    
    // Debug: API test endpoint
    public async Task<IActionResult> TestNewsApi()
    {
        var newsApiEnabled = _configuration.GetValue<bool>("NewsApi:Enabled", false);
        var newsApiKey = _configuration["NewsApi:ApiKey"];
        
        object result;
        
        if (newsApiEnabled && !string.IsNullOrEmpty(newsApiKey) && newsApiKey != "YOUR_API_KEY_HERE")
        {
            try
            {
                var news = await GetNewsFromNewsAPI(newsApiKey);
                result = new
                {
                    Enabled = newsApiEnabled,
                    HasApiKey = true,
                    ApiKeyLength = newsApiKey.Length,
                    News = news.Select(n => new
                    {
                        n.Title,
                        n.Summary,
                        n.Link,
                        n.PublishDate
                    }).ToList()
                };
            }
            catch (Exception ex)
            {
                result = new
                {
                    Enabled = newsApiEnabled,
                    HasApiKey = true,
                    ApiKeyLength = newsApiKey.Length,
                    Error = ex.Message,
                    News = new List<object>()
                };
            }
        }
        else
        {
            result = new
            {
                Enabled = newsApiEnabled,
                HasApiKey = !string.IsNullOrEmpty(newsApiKey) && newsApiKey != "YOUR_API_KEY_HERE",
                ApiKeyLength = newsApiKey?.Length ?? 0,
                News = new List<object>()
            };
        }
        
        return Json(result);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
