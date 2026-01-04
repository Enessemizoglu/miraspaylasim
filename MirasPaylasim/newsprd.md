1) MİRAS HUKUKU ÖZEL HABER FİLTRESİ (En Güçlü Versiyon)

Bu sorgu yalnızca:

miras hukuku

veraset

tereke

intikal

saklı pay

vasiyet

miras paylaşımı

yargıtay miras kararları

ile ilgili haberleri çeker.

✅ NewsAPI Query (everything endpoint – önerilen)
https://newsapi.org/v2/everything?
q=(
   "miras hukuku"
   OR miras
   OR "miras paylaşımı"
   OR veraset
   OR tereke
   OR "intikal işlemleri"
   OR "saklı pay"
   OR vasiyet
   OR "miras davaları"
   OR "yargıtay miras kararı"
   OR "miras sözleşmesi"
 )
 AND NOT (
   spor OR magazin OR dizi OR film OR kaza OR siyaset OR borsa OR kripto
 )
&language=tr
&sortBy=publishedAt
&apiKey=YOUR_API_KEY

🔥 Bu filtre:

Sadece miras hukuku içeriklerini çeker

Haberleri bilgilendirici seviyeye kadar daraltır

Siyaset, spor, kripto gibi tüm gürültüleri komple temizler

Türkçe filtreli olduğu için yerel kaynaklara odaklıdır

🟣 2) MAL REJİMİ TASFİYESİ ÖZEL HABER FİLTRESİ

Bu filtre:

Edinilmiş mallara katılma

Mal ayrılığı

Mal ortaklığı

Katılma alacağı

Mal tasfiyesi davası

Tasfiyede değer artış payı

Boşanma mali sonuçları

gibi konuları hedefler.

✅ NewsAPI Query (everything endpoint – hukuk odaklı)
https://newsapi.org/v2/everything?
q=(
   "mal rejimi"
   OR "mal rejimi tasfiyesi"
   OR "edinilmiş mallara katılma"
   OR "katılma alacağı"
   OR "boşanma mal paylaşımı"
   OR "mal ayrılığı"
   OR "mal ortaklığı"
   OR "edinilmiş mal"
   OR "değer artış payı"
   OR "mal tasfiyesi davası"
   OR "yargıtay mal rejimi kararı"
 )
 AND NOT (
   spor OR magazin OR dizi OR film OR siyaset OR borsa OR kripto OR ekonomi
 )
&language=tr
&sortBy=publishedAt
&apiKey=YOUR_API_KEY

🔥 Bu filtre:

Mal rejimi üzerindeki tüm haberleri, açıklamaları, yargıtay kararlarını bulur

Boşanma sonrası mal paylaşımı konularını da kapsar

Hukuk dışı içerikleri tamamen temizler

Türkçe hukuk içeriklerine odaklanır

🟦 İKİSİNİ BİRLEŞTİRİP TEK SORGU HALİNE GETİRMEK İSTERSEN

Eğer hem miras hukuku hem mal rejimi haberlerini TEK API çağrısıyla çekmek istersen 👇

https://newsapi.org/v2/everything?
q=(
   "miras hukuku"
   OR miras
   OR "miras paylaşımı"
   OR veraset
   OR tereke
   OR "saklı pay"
   OR vasiyet
   OR "yargıtay miras kararı"
   OR
   "mal rejimi"
   OR "mal rejimi tasfiyesi"
   OR "edinilmiş mallara katılma"
   OR "katılma alacağı"
   OR "boşanma mal paylaşımı"
   OR "mal tasfiyesi davası"
   OR "yargıtay mal rejimi kararı"
 )
 AND NOT (
   spor OR magazin OR dizi OR film OR siyaset OR borsa OR ekonomi OR kripto OR kaza
 )
&language=tr
&sortBy=publishedAt
&apiKey=YOUR_API_KEY


Bu filtre sadece hukuk haberlerini, özellikle miras + mal rejimi odaklı olanları çeker.

🔥 BONUS: C# İçinde Hazır Kullanım Kodları
Miras Hukuku Kod
var url = "https://newsapi.org/v2/everything?" +
          "q=(\"miras hukuku\" OR miras OR \"miras paylaşımı\" OR veraset OR tereke OR vasiyet)" +
          " AND NOT (spor OR magazin OR dizi OR film OR siyaset)" +
          "&language=tr&sortBy=publishedAt&apiKey=YOUR_API_KEY";

var client = new HttpClient();
var result = await client.GetStringAsync(url);

Mal Rejimi Tasfiyesi Kod
var url = "https://newsapi.org/v2/everything?" +
          "q=(\"mal rejimi\" OR \"katılma alacağı\" OR \"edinilmiş mallara katılma\" OR \"mal rejimi tasfiyesi\")" +
          " AND NOT (spor OR magazin OR dizi OR film OR siyaset)" +
          "&language=tr&sortBy=publishedAt&apiKey=YOUR_API_KEY";