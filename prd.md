1) Kapsamı netleştirme (girdi ve ekranlar)

Varlık/borç/alıcak toplamı girilecek; net tereke = (malvarlığı + alacaklar – borçlar). Bu alanlar “Malvarlığı”, “Alacağı”, “Borcu” olarak açıkça listelenmiş.

Eşin durumu: “Sağ / Sağ değil” seçimi; “boşanmışlarsa sağ değil işaretlenir” notu var.

Çocuk bilgileri: “Hayatta olan kaç çocuğu var?”, “Daha önce vefat eden çocuk var mı?” (var/yok) ve gerekiyorsa metin girişi.

Torun bilgisi: “Kendinden önce vefat eden çocuğundan torunu var mı?” seçimi.

Anne-baba sağ mı? (sağ/sağ değil).

KVKK onayı metni ve “Onaylıyorum” kutusu.

2) İş kuralları (dosyalardaki miras mantığı)

Zümre sistemi: Altsoy (çocuk/torun), yoksa ana-baba ve onların altsoyu, onlar da yoksa büyükbaba-büyükanne ve kollara geçiş. “Önce ölen mirasçının payı kendi mirasçılarına geçer (temsilen ‘halefiyet’).”

Eşin payı – altsoy varsa: Eş 1/4, kalan 3/4 altsoya eşit bölünür.

Eşin payı – ikinci zümre ile: Eş 1/2, kalan 1/2 ikinci zümreye (anne/baba ve kolları). Notta örnek dağılım belirtilmiş.

Eşin payı – üçüncü zümre ile: Eş 3/4, üçüncü zümre 1/4.

Altsoy paylaşımı: “Altsoy tek başınaysa hepsini alır; birden çok altsoy varsa eşit paylaştırır.” (Eşin payı ayrıldıktan sonra kalan üzerinden).

3) UX akışı (sihirbaz ekranları)

“Miras Hesaplama Aracı” bir sihirbaz gibi ilerlesin: önce Eş: Sağ / Sağ Değil, eş sağ değilse “mirasçıdan önce mi/sonra mı vefat etti?” alt seçenekleri çıksın; ardından çocuk/torun adımları; en sonda ebeveynler. Bu akış birebir dokümanda tarifli.

4) Teknoloji ve proje iskeleti

.NET 8 + ASP.NET Core MVC, EF Core (SQL Server veya hızlı başlamak için SQLite).

Katmanlar: Web (MVC), Domain (entity & kurallar), Application (servisler/validation), Infrastructure (EF Core).

Kurulum:

dotnet new mvc -n MirasPaylasim

dotnet add package Microsoft.EntityFrameworkCore + Microsoft.EntityFrameworkCore.Design + Microsoft.EntityFrameworkCore.SqlServer (veya Sqlite)

Test için: xunit, FluentAssertions.

Doğrulama: FluentValidation.

Kod düzeni: Controller → Service (IInheritanceCalculator) → Domain kuralları → EF DbContext.

5) Veri modeli (Entity’ler)

Calculation (Hesaplama): Id, CreatedAt, TotalAssets, Receivables, Debts, NetEstate (computed), SpouseStatus (Alive/NotAlive), SpouseDeathTiming (Before/After/NA), LivingChildrenCount, HasPredeceasedChild, PredeceasedChildNotes, HasGrandchildrenFromPredeceasedChild, ParentsAlive (Both/MotherOnly/FatherOnly/None), ConsentGiven. (Alanlar proje formundakilerle birebir örtüşüyor. )

Share (Pay): Id, CalculationId, HeirType (Spouse/Child/Grandchild/Parent/… ), HeirDisplay, FractionNumerator, FractionDenominator, Amount.

HeirLine (opsiyonel): temsilen intikal (predeceased → grandchildren) zinciri için.

6) Hesaplama servisi (iş mantığı adımları)

IInheritanceCalculator.Calculate(Calculation input)

net = assets + receivables - debts

Zümre tespiti:

Altsoy varsa → 1. zümre; yoksa ebeveyn/2. zümre; o da yoksa 3. zümre. (Halefiyet kuralını uygula: önce ölen çocuğun payı torunlarına).

Eşin mirasçılığı:

Eş sağ ise payını zümreye göre ata: 1/4 (1. zümre), 1/2 (2. zümre), 3/4 (3. zümre).

Eş sağ değil ise: “mirasçıdan önce vefat” → eş mirasçı değil; “mirasçıdan sonra vefat” → eş mirasçı kabul edilir ve payı eşin tereke tarafına geçer (UI’da bu nedenle iki seçenek var).

Kalan payın dağıtımı:

zümre: çocuklar arasında eşit; tek çocuk varsa hepsi onda (eş payı ayrıldıktan sonra kalan).

zümre: anne-baba hattı; her hat eşitse yarı yarıya; örnek dağılım notları var.

zümre: büyükbaba/büyükanne kolları; eş varsa 1/4 bunlara.

Tutar hesabı: Her pay için Amount = Fraction * net.

Sonuç modeli: paylar listesi, kesir ve TL tutarı; ayrıca kısa açıklama metni.

7) MVC uçtan uca iskelet (somut adımlar)

Proje oluştur: dotnet new mvc -n MirasPaylasim

DbContext & Entity’ler: Calculation, Share; MirasContext : DbContext + DbSet<Calculation>, DbSet<Share>.

Migrations: dotnet ef migrations add InitialCreate → dotnet ef database update.

Controller (CalculatorController):

GET /calculator/start (adım 0: KVKK onayı) → Onay zorunlu.

GET/POST /calculator/estate (malvarlığı/borç/alacak)

GET/POST /calculator/spouse (eş sağ/sağ değil + önce/sonra vefat)

GET/POST /calculator/children (sayı + önce vefat eden çocuk var mı? torun var mı?)

GET/POST /calculator/parents (anne-baba sağ mı?)

POST /calculator/compute → IInheritanceCalculator.Calculate

GET /calculator/result/{id} → paylar tablosu + PDF/CSV indirme.

View’lar: Razor Pages ile çok adımlı form (progress bar), doğrulama mesajları.

Validation: FluentValidation’da sayısal alanlar ≥ 0, mantıksal tutarlılık (ör. “Önce vefat eden çocuk var” seçildiyse torun sorusu aktif).

Unit Testler:

“Eş sağ + 2 çocuk” → eş 1/4; her çocuk 3/8.

“Eş sağ + altsoy yok + ebeveyn var” → eş 1/2; ebeveyn hattı 1/2.

“Eş sağ + 3. zümre” → eş 3/4; üçüncü zümre 1/4.

“Önce ölen çocuk + torun” → halefiyet kontrolü.

8) Güvenlik, KVKK, hukuki not

KVKK metnini sonuç üretmeden zorunlu onay koşulu yap; logla (timestamp/ip).

Çıktıya “Bu araç hukuki danışmanlık değildir” uyarısı ekleyelim.

Basit audit log (hangi alanlar girildi, sonuç ID).

9) Dağıtım ve DevOps

Ortamlar: Dev (SQLite) → Prod (SQL Server veya Azure SQL).

CI/CD: GitHub Actions ile build/test; dotnet ef database update adımı.

Gözlemleme: Serilog + AppInsights (opsiyonel).

10) Yol haritası (öneri)

Gün 1–2: Proje iskeleti, veri modeli, EF migrasyonları.

Gün 3–4: Sihirbaz form ekranları + doğrulama (estate/spouse/children/parents).

Gün 5: Hesaplama servisi (kuralların kodlanması) + Unit testler.

Gün 6: Sonuç ekranı (paylar tablosu, TL ve kesir), indirme (PDF/CSV).

Gün 7: KVKK/onboarding, loglama, hata ekranları, stil.

Gün 8: Paketleme & prod’a ilk deploy.

Hemen başlayalım — ilk görevler

.NET 8 MVC projesini oluştur ve EF paketlerini ekle.

Calculation ve Share entity’lerini yaz; MirasContext’i kur; InitialCreate migrasyonu çalıştır.

IInheritanceCalculator arayüzü + InheritanceCalculator sınıfını aç; yukarıdaki kuralları sırayla uygula (zümre tespiti → eşin payı → kalan dağıtım).

CalculatorController için adımları ekle; her adım formunu dosyadaki alan isimleriyle eşleştir.

İstersen bir sonraki mesajımda Calculation/Share sınıflarının ve DbContext’in temiz bir örnek iskeletini ve InheritanceCalculator için yalın bir pseudocode → C# dönüşümünü çıkarayım.