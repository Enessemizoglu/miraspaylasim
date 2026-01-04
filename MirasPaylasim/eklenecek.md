Miras Hesaplama Sistemine İlişkin PRD (Product Requirements Document)
1. Giriş

Bu doküman, miras hesaplama sürecindeki farklı ekranların hangi koşullarda aktif olacağına dair bir yol haritası sunmaktadır. Kullanıcıların, her zümre ve ilişkili bilgilere göre ekranların sırasıyla nasıl yönlendirileceği detaylandırılacaktır.

2. Genel Akış

Sistem, kullanıcının giriş verilerine göre ekranlar arasında geçiş yapacak ve her zümre ile ilgili bilgilere göre kullanıcıyı yönlendirecektir. Ekranlar, belirli koşullara göre gösterilecek ve kullanıcının doğru bilgileri girmesine olanak tanıyacaktır.

3. Zümre Ekranlarının Gösterilme Koşulları
Birinci Zümre: Eş + Çocuklar/Torunlar

Ekranın Gösterileceği Zaman:

Eşin Durumu: Ekran, miras bırakanın eşinin sağ olup olmadığına göre gösterilir.

Eğer eş sağsa: Birinci zümre ekranı gösterilir.

Eğer eş sağ değilse: İkinci zümre ekranına geçilir.

Çocuklar: Miras bırakanın sağ kalan çocukları varsa, onların sayısı ve durumu (ölü olup olmadıkları) da ekranda belirtilir.

Torunlar: Eğer çocuklardan bir veya daha fazlası ölmüşse, torunlar devreye girer.

Gerekli Alanlar:

Eşin durumu (sağ/sağ değil)

Çocuk sayısı ve durumu (hayatta/ölü)

Torun sayısı (eğer çocuklar ölmüşse)

İkinci Zümre: Kardeşler ve Yeğenler

Ekranın Gösterileceği Zaman:

Anne ve Baba Durumu: Eğer birinci zümredeki eş sağ değilse ve miras bırakanın anne ve babası hayattaysa, ikinci zümre ekranı gösterilir.

Kardeşler: Miras bırakanın kardeşlerinin sağ olup olmadığı ve ölen kardeşlerin çocuklarının (yeğenlerin) olup olmadığı sorgulanır. Eğer bir kardeş ölmüşse, yeğenler devreye girer.

Gerekli Alanlar:

Anne ve baba durumu (sağ/sağ değil)

Kardeş sayısı (sağ kalan)

Ölmüş kardeşlerin çocukları (yeğenler)

Üçüncü Zümre: Dede, Anneanne ve Amca/Teyze/Kuşaklar

Ekranın Gösterileceği Zaman:

İkinci Zümre Durumu: Eğer ikinci zümrede herhangi bir mirasçı yoksa ve birinci zümre (eş) de sağ değilse, üçüncü zümre ekranı aktif hale gelir.

Anneanne, Babaanne, Dede: Üçüncü zümre, daha uzak akrabaların miras paylarını hesaplarken devreye girer. Burada, dört ayrı kök (anneanne, babaanne, dede - anne ve baba tarafı) ve onların alt zümreleri (amca, hala, dayı, teyze) hesaplanır.

Gerekli Alanlar:

Dede ve anneanne durumu (sağ/sağ değil)

Köklerin alt zümreleri (amca, hala, dayı, teyze)

Köklerin sağ kalan bireyleri ve ölenlerin payları

4. Ekranlar Arası Geçiş ve Koşullar
Ekran	Gösterilme Koşulu	Sonraki Ekran	Koşullar
Eş ve Çocuklar/Torunlar	- Eş sağ ise ve çocuklar varsa	İkinci Zümre (Kardeşler)	- Eş sağ değilse, ikinci zümre ekranı gösterilir
İkinci Zümre (Kardeşler)	- Eş sağ değilse ve anne-baba sağ ise	Üçüncü Zümre (Dede/Anneanne)	- Anne-baba sağ değilse ve çocuklar da ölmüşse, üçüncü zümre ekranı gösterilir
Üçüncü Zümre (Dede)	- Eş ve çocuklar (torunlar) yoksa, ikinci zümre de yoksa, üçüncü zümre aktif olur	Sonuç Sayfası	- Eğer hiçbir zümre mevcut değilse ve eş de sağ değilse, miras hazineye gider
5. Ekstra Bilgiler ve Gereksinimler
Test Senaryoları:

Test 1: Eş sağ, 2 çocuk, 1 torun (1 çocuk ölmüş)

Ekran Akışı: Birinci Zümre → Eş ve çocuklar bilgileri girilir. Torun payı hesaplanır.

Test 2: Eş sağ değil, 2 çocuk (biri ölü), 1 torun

Ekran Akışı: İkinci Zümre → Kardeşlerin ve yeğenlerin durumu sorgulanır. Miras payı hesaplanır.

Test 3: Eş sağ değil, çocuklar ölü, anne-baba sağ

Ekran Akışı: İkinci Zümre → Kardeşlerin sayısı ve yeğen payları hesaplanır.

6. Kullanıcı Deneyimi ve Görseller

Her ekranın sonunda, kullanıcıya bir sonraki adım için yönlendirme yapılacaktır.

Gerekli alanlar ve veriler, her zümreye göre açıkça belirtilecektir.