using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MirasPaylasim.Models;

namespace MirasPaylasim.Services;

public class GeminiNewsFilterService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GeminiNewsFilterService> _logger;
    
    public GeminiNewsFilterService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<GeminiNewsFilterService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }
    
    public async Task<List<NewsApiArticle>> FilterInheritanceNews(
        List<NewsApiArticle> articles, 
        int maxResults = 3)
    {
        var geminiEnabled = _configuration.GetValue<bool>("GeminiApi:Enabled", false);
        var geminiApiKey = _configuration["GeminiApi:ApiKey"];
        
        // HİBRİT FİLTRELEME: Önce basit keyword filtreleme yap
        var preFiltered = PreFilterWithKeywords(articles);
        _logger.LogInformation("Ön filtreleme: {Count} haber bulundu", preFiltered.Count);
        
        // Eğer ön filtreleme yeterli haber bulduysa, direkt döndür (Gemini'ye gitme - hızlı!)
        if (preFiltered.Count >= maxResults)
        {
            var selected = preFiltered.Take(maxResults).ToList();
            _logger.LogInformation("Ön filtreleme yeterli, Gemini'ye gidilmiyor. {Count} haber seçildi", selected.Count);
            return selected;
        }
        
        if (!geminiEnabled || string.IsNullOrEmpty(geminiApiKey))
        {
            _logger.LogInformation("Gemini API devre dışı, basit filtreleme kullanılıyor");
            return preFiltered.Take(maxResults).ToList();
        }
        
        try
        {
            // Sadece şüpheli haberleri Gemini'ye gönder (5-7 haber - daha hızlı!)
            // Şüpheli = "miras" kelimesi geçiyor ama "kültürel miras" gibi olabilir
            var suspiciousArticles = FindSuspiciousArticles(articles, preFiltered);
            var articlesToAnalyze = suspiciousArticles.Take(7).ToList();
            
            if (!articlesToAnalyze.Any())
            {
                _logger.LogInformation("Şüpheli haber bulunamadı, ön filtreleme sonuçları kullanılıyor");
                return preFiltered.Take(maxResults).ToList();
            }
            
            _logger.LogInformation("Gemini'ye {Count} şüpheli haber gönderiliyor...", articlesToAnalyze.Count);
            
            var prompt = BuildAnalysisPrompt(articlesToAnalyze);
            var analysis = await AnalyzeWithGemini(prompt, geminiApiKey);
            
            // Gemini yanıtını logla
            if (!string.IsNullOrEmpty(analysis))
            {
                _logger.LogInformation("Gemini yanıtı: {Response}", analysis.Substring(0, Math.Min(200, analysis.Length)));
            }
            
            // Gemini'nin döndürdüğü indekslere göre haberleri seç
            var selectedIndices = ParseGeminiResponse(analysis);
            var geminiSelected = new List<NewsApiArticle>();
            
            _logger.LogInformation("Gemini parse sonucu: {Count} indeks bulundu: {Indices}", 
                selectedIndices.Count, string.Join(",", selectedIndices));
            
            foreach (var index in selectedIndices.Take(maxResults))
            {
                if (index >= 0 && index < articlesToAnalyze.Count)
                {
                    geminiSelected.Add(articlesToAnalyze[index]);
                }
            }
            
            _logger.LogInformation("Gemini API {Count} haber seçti", geminiSelected.Count);
            
            // Ön filtreleme + Gemini sonuçlarını birleştir
            var finalArticles = new List<NewsApiArticle>();
            finalArticles.AddRange(preFiltered);
            
            // Gemini'nin seçtiği haberleri ekle (duplicate kontrolü ile)
            var existingUrls = finalArticles.Select(a => a.Url).ToList();
            var newFromGemini = geminiSelected.Where(a => !existingUrls.Contains(a.Url)).ToList();
            finalArticles.AddRange(newFromGemini);
            
            // Eğer hala yeterli değilse, basit filtreleme ile tamamla
            if (finalArticles.Count < maxResults)
            {
                var remaining = articles.Except(finalArticles).ToList();
                var additional = FilterWithSimpleKeywords(remaining, maxResults - finalArticles.Count);
                finalArticles.AddRange(additional);
                _logger.LogInformation("Basit filtreleme ile {Count} ek haber eklendi", additional.Count);
            }
            
            return finalArticles.Take(maxResults).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gemini API hatası, ön filtreleme sonuçları kullanılıyor");
            return preFiltered.Take(maxResults).ToList();
        }
    }
    
    // Ön filtreleme: Güçlü keyword'lerle direkt miras hukuku haberlerini bul
    private List<NewsApiArticle> PreFilterWithKeywords(List<NewsApiArticle> articles)
    {
        // Güçlü keyword'ler - bunlar kesinlikle miras hukuku ("miras hukuku" kelimesi şart değil)
        var strongKeywords = new[] { 
            "miras paylaşımı", "miras paylaşım", "veraset", "tereke", 
            "vasiyet", "saklı pay", "miras davası", "yargıtay miras",
            "miras sözleşmesi", "intikal işlemleri", "mirasçılık",
            "zümre", "halefiyet", "medeni kanun miras", "mirasçı",
            "miras payı", "veraset ilamı", "miras hukuku", "tenkis",
            "miras bırakan", "mirasçı hakları", "miras reddi"
        };
        
        // False positive'ler - bunlar kesinlikle miras hukuku DEĞİL
        var falsePositives = new[] { 
            "kültürel miras", "tarihi miras", "doğal miras", 
            "unesco", "balkan", "osmanlı eserleri", "kütüphane",
            "turizm", "korkuluk", "müze", "sanat", "kültür",
            "barbaros", "hatay", "makedonya", "izmir", "urla",
            "deprem", "dünya mirası", "arkeolojik", "tarih"
        };
        
        return articles
            .Where(article => 
            {
                if (string.IsNullOrEmpty(article.Title) && string.IsNullOrEmpty(article.Description))
                    return false;
                    
                var text = $"{article.Title} {article.Description}".ToLowerInvariant();
                
                // ÖNCE: False positive kontrolü - bunlar varsa kesinlikle reddet
                if (falsePositives.Any(fp => text.Contains(fp)))
                {
                    return false; // Kesinlikle reddet
                }
                
                // SONRA: Güçlü keyword kontrolü - bunlardan biri geçmeli
                var hasStrongKeyword = strongKeywords.Any(keyword => text.Contains(keyword));
                
                if (hasStrongKeyword)
                {
                    return true; // Kesinlikle kabul et
                }
                
                // Eğer sadece "miras" kelimesi geçiyorsa ve false positive yoksa, şüpheli olarak işaretle
                // (Bu haberler Gemini'ye gönderilecek)
                if (text.Contains("miras") && !text.Contains("kültürel") && !text.Contains("tarihi"))
                {
                    return false; // Şüpheli, Gemini'ye gönder
                }
                
                return false;
            })
            .ToList();
    }
    
    // Şüpheli haberleri bul: "miras" kelimesi geçiyor ama kesin değil
    private List<NewsApiArticle> FindSuspiciousArticles(List<NewsApiArticle> allArticles, List<NewsApiArticle> preFiltered)
    {
        var preFilteredUrls = preFiltered.Select(a => a.Url).ToList();
        
        return allArticles
            .Where(article => !preFilteredUrls.Contains(article.Url))
            .Where(article => 
            {
                var text = $"{article.Title} {article.Description}".ToLowerInvariant();
                
                // "miras" kelimesi geçiyor ama güçlü keyword yok
                return text.Contains("miras") && 
                       !text.Contains("miras hukuku") &&
                       !text.Contains("miras paylaşımı") &&
                       !text.Contains("veraset") &&
                       !text.Contains("tereke");
            })
            .ToList();
    }
    
    private string BuildAnalysisPrompt(List<NewsApiArticle> articles)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Aşağıda Türkçe haberlerin başlık ve özetleri verilmiştir. ");
        sb.AppendLine("Bu haberlerden SADECE MİRAS HUKUKU (veraset hukuku) ile ilgili olanları seçmen gerekiyor.");
        sb.AppendLine();
        sb.AppendLine("✅ MİRAS HUKUKU KAPSAMINDA OLANLAR:");
        sb.AppendLine("- Miras paylaşımı, veraset, tereke, intikal işlemleri");
        sb.AppendLine("- Vasiyet, saklı pay, mirasçılık, mirasçı hakları");
        sb.AppendLine("- Zümre sistemi, miras davaları, miras sözleşmesi");
        sb.AppendLine("- Yargıtay miras kararları, mahkeme miras kararları");
        sb.AppendLine("- Halefiyet, temsilen mirasçılık");
        sb.AppendLine("- Medeni Kanun miras hükümleri, miras payı hesaplama");
        sb.AppendLine();
        sb.AppendLine("❌ MİRAS HUKUKU DEĞİLDİR (KESINLIKLE SEÇME!):");
        sb.AppendLine("- Kültürel miras, tarihi miras, doğal miras");
        sb.AppendLine("- UNESCO mirası, dünya mirası");
        sb.AppendLine("- Osmanlı eserleri, balkan mirası");
        sb.AppendLine("- Kütüphane, müze, turizm, korkuluk");
        sb.AppendLine("- 'Miras' kelimesi geçiyor ama hukukla ilgili değilse SEÇME!");
        sb.AppendLine();
        sb.AppendLine("KURAL: Eğer bir haber 'miras' kelimesi içeriyor ama 'kültürel miras', 'tarihi miras', 'turizm', 'kütüphane', 'balkan', 'osmanlı' gibi kelimeler içeriyorsa, MİRAS HUKUKU DEĞİLDİR!");
        sb.AppendLine();
        sb.AppendLine("Yanıt formatı: Sadece seçtiğin haberlerin numaralarını virgülle ayırarak yaz (örn: 0,2,5)");
        sb.AppendLine("En fazla 3 haber seç. Eğer hiç miras hukuku haberi yoksa, boş bırak.");
        sb.AppendLine();
        sb.AppendLine("Haberler:");
        sb.AppendLine();
        
        for (int i = 0; i < articles.Count; i++)
        {
            var article = articles[i];
            sb.AppendLine($"[{i}] Başlık: {article.Title ?? ""}");
            sb.AppendLine($"     Özet: {article.Description ?? ""}");
            sb.AppendLine();
        }
        
        return sb.ToString();
    }
    
    private async Task<string> AnalyzeWithGemini(string prompt, string apiKey)
    {
        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(15); // Daha hızlı timeout
        
        var model = _configuration["GeminiApi:Model"] ?? "gemini-3-flash-preview";
        
        // Google AI Studio formatına göre request body
        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            }
        };
        
        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        // Doğru endpoint: v1beta/models/{model}:generateContent
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";
        
        _logger.LogInformation("Gemini API'ye istek gönderiliyor (model: {Model}, endpoint: v1beta)...", model);
        
        var response = await client.PostAsync(url, content);
        var responseJson = await response.Content.ReadAsStringAsync();
        
        if (!response.IsSuccessStatusCode)
        {
            var errorMsg = responseJson.Substring(0, Math.Min(500, responseJson.Length));
            _logger.LogWarning("Gemini API hatası: {StatusCode} - {Response}", response.StatusCode, errorMsg);
            
            // Eğer model bulunamazsa, alternatif modelleri dene
            if (responseJson.Contains("not found") || responseJson.Contains("NOT_FOUND"))
            {
                _logger.LogInformation("Model {Model} bulunamadı, alternatif modeller deneniyor...", model);
                
                var alternativeModels = new[] { "gemini-1.5-flash", "gemini-1.5-pro", "gemini-pro" };
                
                foreach (var altModel in alternativeModels)
                {
                    if (altModel == model) continue;
                    
                    _logger.LogInformation("Alternatif model deneniyor: {Model}", altModel);
                    url = $"https://generativelanguage.googleapis.com/v1beta/models/{altModel}:generateContent?key={apiKey}";
                    response = await client.PostAsync(url, content);
                    responseJson = await response.Content.ReadAsStringAsync();
                    
                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("✅ Başarılı! Model: {Model}", altModel);
                        break;
                    }
                }
            }
            
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Gemini API hatası: {response.StatusCode}");
            }
        }
        
        var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        var result = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text ?? "";
        
        // Eğer yanıt boşsa, raw response'u logla
        if (string.IsNullOrEmpty(result))
        {
            _logger.LogWarning("Gemini API boş yanıt döndü. Raw response: {Response}", 
                responseJson.Substring(0, Math.Min(500, responseJson.Length)));
        }
        else
        {
            _logger.LogInformation("Gemini API yanıtı alındı: {Length} karakter", result.Length);
        }
        
        return result;
    }
    
    private List<int> ParseGeminiResponse(string response)
    {
        var indices = new List<int>();
        
        if (string.IsNullOrEmpty(response))
        {
            return indices;
        }
        
        // Gemini'nin yanıtından sayıları çıkar (örn: "0,2,5" veya "Seçtiğim haberler: 0, 2, 5")
        var matches = System.Text.RegularExpressions.Regex.Matches(response, @"\b(\d+)\b");
        
        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            if (int.TryParse(match.Value, out int index))
            {
                indices.Add(index);
            }
        }
        
        return indices.Distinct().OrderBy(x => x).ToList();
    }
    
    private List<NewsApiArticle> FilterWithSimpleKeywords(List<NewsApiArticle> articles, int maxResults)
    {
        var keywords = new[] { 
            "miras", "veraset", "tereke", "vasiyet", "mirasçı", 
            "saklı pay", "miras paylaşımı", "miras hukuku",
            "miras davası", "yargıtay miras", "intikal"
        };
        
        return articles
            .Where(article => 
            {
                var text = $"{article.Title} {article.Description}".ToLowerInvariant();
                return keywords.Any(keyword => text.Contains(keyword));
            })
            .Take(maxResults)
            .ToList();
    }
    
    // Gemini Search Grounding ile direkt Google'dan haber çek
    public async Task<List<NewsItem>> GetNewsFromGeminiSearch(int maxResults = 3)
    {
        var geminiApiKey = _configuration["GeminiApi:ApiKey"];
        // appsettings'den modeli al, yoksa varsayılan olarak 2.5 Flash kullan
        var model = _configuration["GeminiApi:Model"] ?? "gemini-2.5-flash"; 

        if (string.IsNullOrEmpty(geminiApiKey))
        {
            _logger.LogWarning("Gemini API key yok, Search kullanılamıyor");
            return new List<NewsItem>();
        }

        try
        {
            // ÖNEMLİ: Prompt'u JSON formatı isteyecek şekilde değiştirdik
            var prompt = $@"Türkiye'de 'Miras Hukuku' ile ilgili son 1 ayda çıkan en güncel haberleri ve gelişmeleri bul.
        
        ARAMA KRİTERLERİ (En az biri geçmeli):
        - Miras paylaşımı, veraset ilamı, tereke davaları
        - Vasiyetname, saklı pay, tenkis davası
        - Yargıtay'ın yeni miras kararları
        
        HARİÇ TUTULACAKLAR (Bunları getirme):
        - Kültürel miras, tarihi eser, müze, UNESCO, turizm
        
        ÇIKTI FORMATI:
        Bulduğun haberleri aşağıdaki JSON formatında, bir liste (array) olarak ver. 
        Markdown (```json) kullanma, sadece saf JSON ver.
        
        [
          {{
            ""title"": ""Haber Başlığı"",
            ""source"": ""Haber Kaynağı"",
            ""date"": ""Tarih (GG.AA.YYYY formatında)"",
            ""summary"": ""Haberin içeriği ve hukuki boyutu (2 cümle)"",
            ""link"": ""Haber linki""
          }}
        ]
        
        En güncel {maxResults} haberi getir.";

            var requestBody = new
            {
                contents = new[]
                {
                    new { role = "user", parts = new[] { new { text = prompt } } }
                },
                tools = new[]
                {
                    new { googleSearch = new { } } // Google Search Tool Aktif
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(30); 
            
            // API URL yapısı
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={geminiApiKey}";
            
            _logger.LogInformation("Gemini Search ({Model}) ile haber çekiliyor...", model);
            
            var response = await client.PostAsync(url, content);
            var responseJson = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Gemini Hatası: {StatusCode} - {Response}", response.StatusCode, responseJson);
                return new List<NewsItem>();
            }

            // Yanıtı al
            var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            var textResult = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text ?? "";

            // Temizlik: Gemini bazen ```json etiketi ekler, onu siliyoruz
            textResult = textResult.Replace("```json", "").Replace("```", "").Trim();

            _logger.LogInformation("Gemini Ham JSON Cevap: {Response}", textResult);

            // JSON Parse işlemi
            return ParseJsonNews(textResult, maxResults);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gemini Search işlemsel hata");
            return new List<NewsItem>();
        }
    }
    
    // Yeni JSON Parse Metodu
    private List<NewsItem> ParseJsonNews(string jsonResult, int maxResults)
    {
        var news = new List<NewsItem>();
        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            // Gelen string'i listeye çevir
            var items = JsonSerializer.Deserialize<List<JsonNewsDto>>(jsonResult, options);

            if (items != null)
            {
                foreach (var item in items.Take(maxResults))
                {
                    DateTime publishDate;
                    // Tarih parse edilemezse bugünü ver
                    if (!DateTime.TryParse(item.Date, out publishDate))
                    {
                        publishDate = DateTime.Now;
                    }

                    news.Add(new NewsItem
                    {
                        Title = item.Title ?? "Başlıksız Haber",
                        Summary = $"[{item.Source}] {item.Summary}", // Kaynağı özete ekledik
                        Link = item.Link ?? "#",
                        PublishDate = publishDate,
                        ImageUrl = null 
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "JSON Parse Hatası. Gemini geçerli JSON döndürmemiş olabilir.");
        }
        return news;
    }
    
    private List<NewsItem> ParseGeminiSearchResponse(string response, int maxResults)
    {
        var news = new List<NewsItem>();
        
        if (string.IsNullOrEmpty(response))
        {
            return news;
        }
        
        // Gemini'nin yanıtını parse et
        // Format: [1] Başlık: ... \n Kaynak: ... \n Tarih: ... \n Özet: ... \n Link: ...
        
        var lines = response.Split('\n');
        NewsItem? currentItem = null;
        
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            
            if (trimmed.StartsWith("[") && trimmed.Contains("]"))
            {
                // Yeni haber başlıyor
                if (currentItem != null && !string.IsNullOrEmpty(currentItem.Title))
                {
                    news.Add(currentItem);
                }
                currentItem = new NewsItem();
            }
            
            // Başlık parse (herhangi bir yerde olabilir)
            if (currentItem != null && string.IsNullOrEmpty(currentItem.Title))
            {
                var titleMatch = System.Text.RegularExpressions.Regex.Match(trimmed, @"Başlık:\s*(.+?)(?:\n|$)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (titleMatch.Success)
                {
                    currentItem.Title = titleMatch.Groups[1].Value.Trim();
                }
                // Eğer format farklıysa, direkt başlık olabilir
                else if (trimmed.Length > 10 && trimmed.Length < 200 && !trimmed.Contains(":") && !trimmed.StartsWith("["))
                {
                    currentItem.Title = trimmed;
                }
            }
            else if (currentItem != null)
            {
                if (trimmed.StartsWith("Kaynak:", StringComparison.OrdinalIgnoreCase))
                {
                    var source = trimmed.Substring(7).Trim();
                    // Kaynak bilgisini summary'ye ekle
                    if (!string.IsNullOrEmpty(currentItem.Summary))
                    {
                        currentItem.Summary = $"[{source}] {currentItem.Summary}";
                    }
                }
                else if (trimmed.StartsWith("Tarih:", StringComparison.OrdinalIgnoreCase))
                {
                    var dateStr = trimmed.Substring(6).Trim();
                    if (DateTime.TryParse(dateStr, out var date))
                    {
                        currentItem.PublishDate = date;
                    }
                }
                else if (trimmed.StartsWith("Özet:", StringComparison.OrdinalIgnoreCase))
                {
                    currentItem.Summary = trimmed.Substring(5).Trim();
                }
                else if (trimmed.StartsWith("Link:", StringComparison.OrdinalIgnoreCase))
                {
                    currentItem.Link = trimmed.Substring(5).Trim();
                }
                else if (!string.IsNullOrEmpty(trimmed) && string.IsNullOrEmpty(currentItem.Summary))
                {
                    // Eğer özet yoksa, bu satır özet olabilir
                    if (trimmed.Length > 20 && trimmed.Length < 300)
                    {
                        currentItem.Summary = trimmed;
                    }
                }
            }
        }
        
        // Son haberi ekle
        if (currentItem != null && !string.IsNullOrEmpty(currentItem.Title))
        {
            news.Add(currentItem);
        }
        
        // Eğer parse başarısız olduysa, basit regex ile dene
        if (news.Count == 0)
        {
            var titleMatches = System.Text.RegularExpressions.Regex.Matches(response, @"Başlık:\s*([^\n]+)");
            var summaryMatches = System.Text.RegularExpressions.Regex.Matches(response, @"Özet:\s*([^\n]+)");
            var linkMatches = System.Text.RegularExpressions.Regex.Matches(response, @"Link:\s*([^\n]+)");
            
            for (int i = 0; i < Math.Min(titleMatches.Count, maxResults); i++)
            {
                var item = new NewsItem
                {
                    Title = titleMatches[i].Groups[1].Value.Trim(),
                    Summary = i < summaryMatches.Count ? summaryMatches[i].Groups[1].Value.Trim() : "",
                    Link = i < linkMatches.Count ? linkMatches[i].Groups[1].Value.Trim() : "#",
                    PublishDate = DateTime.Now.AddDays(-i)
                };
                
                if (!string.IsNullOrEmpty(item.Title))
                {
                    news.Add(item);
                }
            }
        }
        
        return news.Take(maxResults).ToList();
    }
}

// JSON verisini karşılayacak basit sınıf
public class JsonNewsDto
{
    public string Title { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Link { get; set; } = string.Empty;
}

