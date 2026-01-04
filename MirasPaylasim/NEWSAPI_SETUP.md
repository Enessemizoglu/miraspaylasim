# NewsAPI Kurulum Rehberi

## NewsAPI Nedir?

NewsAPI, dünya çapındaki haber kaynaklarından haberleri çekmenizi sağlayan ücretsiz bir API servisidir.

## API Key Nasıl Alınır?

1. **NewsAPI.org'a Kayıt Olun**
   - https://newsapi.org/register adresine gidin
   - Ücretsiz hesap oluşturun

2. **API Key'i Alın**
   - Giriş yaptıktan sonra Dashboard'dan API key'inizi kopyalayın
   - Ücretsiz plan: Günde 100 istek limiti

3. **API Key'i Yapılandırın**
   - `appsettings.json` dosyasını açın
   - `NewsApi:ApiKey` değerine API key'inizi yapıştırın
   - `NewsApi:Enabled` değerini `true` yapın

## Örnek Yapılandırma

```json
{
  "NewsApi": {
    "ApiKey": "abc123def456ghi789jkl012mno345pqr678",
    "Enabled": true
  }
}
```

## Özellikler

- ✅ Türkçe haberler için otomatik filtreleme
- ✅ "miras hukuku", "miras paylaşımı" gibi anahtar kelimelerle arama
- ✅ 1 saatlik cache (performans için)
- ✅ Hata durumunda otomatik yedek haberler
- ✅ 10 saniye timeout koruması

## Limitler

- **Ücretsiz Plan**: 100 istek/gün
- **Cache**: 1 saat (günlük limiti aşmamak için)
- **Timeout**: 10 saniye

## Sorun Giderme

### API Key Çalışmıyor
- API key'in doğru kopyalandığından emin olun
- NewsAPI dashboard'dan key'in aktif olduğunu kontrol edin

### Haberler Görünmüyor
- `NewsApi:Enabled` değerinin `true` olduğundan emin olun
- Log dosyalarını kontrol edin (hata mesajları için)
- Yedek haberler otomatik olarak gösterilir

### Rate Limit Hatası
- Günlük 100 istek limitini aştınız
- Cache sayesinde aynı haberler 1 saat boyunca gösterilir
- Ertesi gün limit sıfırlanır

