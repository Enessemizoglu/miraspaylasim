2. Zümre Mirasçıları – Gösterim Koşulları, Pay Kuralları ve Teknik Gereksinimler

Bu doküman, MirasPaylaşım uygulamasında İkinci Zümre (2. Zümre) ekranının ne zaman gösterileceğini, hangi verilerin isteneceğini ve pay dağılımının nasıl hesaplanacağını tanımlar.

🔵 1. 2. Zümre Nedir?

zümre;

Anne ve Baba Kökü (2 bağımsız kök)

Ve onların altsoyu:

Kardeşler

Ölmüş kardeşlerin çocukları (Yeğenler) → temsilen pay alır

Her zümre kolu kendi içinde yarım paylık bir kökü temsil eder:

Kök	Pay
Baba kolu	1/2
Anne kolu	1/2

Bu iki kök bağımsızdır.

🔵 2. 2. Zümre Ne Zaman Mirasçı Olur?

zümre ancak şu şartlar sağlandığında mirasçı olabilir:

✔️ Şart 1 — 1. Zümrede hiç kimse olmayacak

Yani:

Çocuk yok

Torun yok

Torunun çocuğu yok

Altsoy tamamen yoksa → 2. zümre devreye girer.

✔️ Şart 2 — “Eş sağ değil” → 2 seçenek

Eş sağ değil seçildiğinde kullanıcıdan şu bilgi istenir:

Mirasçıdan önce vefat etti
-> Evet → 2. zümreye geçilir

Mirasçıdan sonra vefat etti
-> Bu durumda eş mirasçı olamayacağı için yine 2. zümreye geçilir

✔️ Şart 3 — Anne veya Baba veya onların altsoyu hayatta olmalı

Zümre, şunlardan herhangi biri varsa miras alır:

Anne sağ

Baba sağ

Kardeş

Yeğen (ölen kardeşin çocuğu)

Eğer anne-baba da yok, kardeş-yeğen de yok → 3. zümreye geçilir.

🔵 3. 2. Zümre Ekranı Ne Zaman Gösterilir?

Gösterim mantığı:

if (!altSoyVar) 
    ikinciZümreEkranı.Göster()
else
    ikinciZümreEkranı.Gösterme()


Yani:

1. ZÜMRE	2. ZÜMRE EKRANI
❌ yok	✔️ AÇILIR
✔️ var	❌ açılmaz
🔵 4. 2. Zümrede Paylaştırma Kuralları
🟣 4.1. Eşin Durumu
Eş Durumu	Eş Payı	2. Zümrenin Payı
Eş sağ ise	1/2	1/2
Eş sağ değil	0	1 (tamamı)
🟣 4.2. Kök Payı Mantığı (Stem Method)

2 zümrede iki bağımsız kök vardır:

1) Baba Kolu = 1/2
2) Anne Kolu = 1/2

Her kol kendi içinde şöyle işler:

🟣 4.3. Baba veya Anne Sağsa
✔️ Eğer anne veya baba sağsa → pay doğrudan ona gider.

Örnek:

Baba sağ → 1/2 alır

Anne sağ değil → anne kolundaki 1/2 kardeşlere/yeğenlere dağılır

🟣 4.4. Anne/Baba ölmüşse → kardeşlere geçilir

Yaşayan kardeşler → payı eşit alır

Ölmüş kardeşin payı → yeğenlerine temsilen geçer

Temsilen iniş (representation) kuralı uygulanır.

🟣 4.5. Kök tamamen boşsa

Örnek:
Anne yok → kardeş yok → yeğen yok

➡️ Anne kolu pasif olur.

Bu durumda baba kolu aktif olduğu için baba kolu tamamını alır.

🔵 5. Paylaştırma Örnekleri (Test Senaryoları)
🟩 Örnek 1 — Eş var, baba sağ, anne ölmüş, 2 kardeş + 1 ölmüş kardeşten 2 yeğen

Eş = 1/2

zümre = 1/2

Dağılım:

Baba Kolu (1/2 → tamamını baba alır)

Baba = 1/2

Anne Kolu (1/2 → kardeşlere dağıtılır)

Toplam kişi:

2 sağ kardeş

1 ölü kardeş → 2 yeğen

Toplam kök sayısı: 3

Anne kolu hesaplama:

Her kök = 1/2 / 3 = 1/6

Ölen kardeşin payı (1/6) = 2 yeğene eşit → 1/12 + 1/12

Sonuç:

Eş = 1/2

Baba = 1/2

Sağ kardeşler = 1/6 + 1/6

Yeğenler = 1/12 + 1/12

🟩 Örnek 2 — Eş yok, anne-baba ölü, 1 kardeş, 1 ölmüş kardeş → 1 yeğen

Aktif kökler:

Baba kökü → kardeş + yeğen var → aktif

Anne kökü → kardeş + yeğen var → aktif

Pay:

2. zümre = 1 (tamamı)

Toplam 2 kök → her biri 1/2

Anne kolu:

1 sağ kardeş, 1 ölü kardeş → pay: 1/2

Ölen kardeş → yeğen payı = 1/4

Sağ kardeş → 1/2 / 2 = 1/4

🟩 Örnek 3 — Anne ölü, anne kolu tamamen boş, baba sağ değil, kardeş yok

Aktif sadece baba kolu.

zümre = 1 (tamamı)

Baba kolu = 1 alır

🔵 6. 2. Zümre Ekranı İçin Form Alanları
Genel Alanlar:

Anne sağ mı? (E/H)

Baba sağ mı? (E/H)

Anne Kolu:

Yaşayan kardeş sayısı

Ölmüş kardeşlerin çocuk sayıları (dinamik liste)

Baba Kolu:

Yaşayan kardeş sayısı

Ölmüş kardeşlerin çocuk sayıları (dinamik liste)

🔵 7. Backend Gereksinimleri

Her kol (stem) ayrı işlenmelidir:

class SecondOrderStem
{
    public bool RootAlive { get; set; } 
    public int AliveSiblings { get; set; }
    public List<int> PredeceasedSiblingsChildrenCounts { get; set; }
}


Ana fonksiyon:

DistributeToStems(halfShareAmount, SecondOrderStem stem);


Ana algoritma:

Kök sağsa → tüm payı alır

Değilse:

sağ kardeşlere eşit

ölen kardeşlerin payı çocuklarına (yeğenlere) temsilen iner

Altsoyu yoksa → kök pasiftir

Pasif köklerin payı aktif kökler arasında eşit bölünür.

🔵 8. Özet
Kural	Açıklama
2. zümre sadece 1. zümre tamamen boşsa devreye girer	✔️
Anne ve baba kendi başına birer köktür	✔️
Eş varsa 1/2'si eşe, 1/2’si ikinci zümreye	✔️
Kök sağsa direkt alır	✔️
Kök ölmüşse kardeş-yeğenlere iner	✔️
Kök tamamen boşsa → pasif, pay diğer köklere gider	✔️