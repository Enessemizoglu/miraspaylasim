using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using MirasPaylasim.Models;

namespace MirasPaylasim.Services
{
    public class InheritanceCalculator : IInheritanceCalculator
    {
        public List<Share> Calculate(Calculation input)
        {
            var shares = new List<Share>();

            // 1. ADIM: SANAL TEREKEYİ OLUŞTURMA
            decimal totalGifts = 0;
            var hotchpotItems = GetHotchpotItems(input);
            System.Diagnostics.Debug.WriteLine($"=== DENKLEŞTİRME DEBUG ===");
            System.Diagnostics.Debug.WriteLine($"HasAssistance: {input.HasAssistance}");
            System.Diagnostics.Debug.WriteLine($"AssistanceList: {input.AssistanceList}");
            System.Diagnostics.Debug.WriteLine($"Hotchpot items sayısı: {hotchpotItems.Count}");
            foreach (var item in hotchpotItems)
            {
                System.Diagnostics.Debug.WriteLine($"  - {item.HeirName}: {item.EstimatedValue:N2} TL (Mirastan Say: {item.CountAsInheritance})");
                totalGifts += item.EstimatedValue;
            }
            System.Diagnostics.Debug.WriteLine($"Toplam denkleştirme: {totalGifts:N2} TL");

            // Sanal Tereke = Mevcut Para + İade Edilecek Hediyeler
            decimal virtualEstate = input.NetEstate + totalGifts;

            System.Diagnostics.Debug.WriteLine($"=== HESAPLAMA BAŞLANGICI ===");
            System.Diagnostics.Debug.WriteLine($"Net Tereke: {input.NetEstate:N2} TL");
            System.Diagnostics.Debug.WriteLine($"Toplam Denkleştirme: {totalGifts:N2} TL");
            System.Diagnostics.Debug.WriteLine($"Sanal Tereke: {virtualEstate:N2} TL");

            // 2. ADIM: ZÜMRE VE EŞ DURUMU BELİRLEME
            bool spouseAlive = input.SpouseStatus == SpouseStatus.Alive ||
                              (input.SpouseStatus == SpouseStatus.NotAlive && input.SpouseDeathTiming == SpouseDeathTiming.AfterDeceased);

            bool hasFirstDegree = HasFirstDegreeHeirs(input);
            bool hasSecondDegree = HasSecondDegreeHeirs(input);
            bool hasThirdDegree = HasThirdDegreeHeirs(input);

            decimal spouseShare = 0;
            decimal remainingEstateForZumre = virtualEstate;

            // 3. ADIM: EŞİN PAYINI AYIRMA
            if (spouseAlive)
            {
                if (hasFirstDegree)
                    spouseShare = virtualEstate * 0.25m; // 1/4
                else if (hasSecondDegree)
                    spouseShare = virtualEstate * 0.50m; // 1/2
                else if (hasThirdDegree)
                    spouseShare = virtualEstate * 0.75m; // 3/4
                else
                    spouseShare = virtualEstate; // Tamamı

                shares.Add(new Share
                {
                    HeirType = HeirType.Spouse,
                    HeirDisplay = "Eş",
                    Amount = spouseShare,
                    TheoreticalAmount = spouseShare // Saklı pay hesabı için ham tutar
                });

                remainingEstateForZumre = virtualEstate - spouseShare;
            }
            else
            {
                remainingEstateForZumre = virtualEstate;
            }

            // 4. ADIM: ZÜMRE PAYLAŞIMI (Hukuksal Oranlara Göre)
            if (remainingEstateForZumre > 0)
            {
                if (hasFirstDegree)
                {
                    DistributeFirstDegree(remainingEstateForZumre, input, shares);
                }
                else if (hasSecondDegree)
                {
                    DistributeSecondDegree(remainingEstateForZumre, input, shares);
                }
                else if (hasThirdDegree)
                {
                    DistributeThirdDegree(remainingEstateForZumre, input, shares);
                }
                else if (!spouseAlive)
                {
                    shares.Add(new Share
                    {
                        HeirType = HeirType.Other,
                        HeirDisplay = "Hazine",
                        Amount = input.NetEstate,
                        TheoreticalAmount = input.NetEstate
                    });
                }
            }

            // 5. ADIM: MAHSUP (DENKLEŞTİRME) İŞLEMİ
            ApplyDeductions(shares, hotchpotItems);

            // 6. ADIM: SAKLI PAY VE TENKİS HESAPLAMASI
            CalculateReservedPortions(shares, hasFirstDegree, hasSecondDegree);

            // 7. ADIM: KESİRLERİ DÜZENLE
            NormalizeFractions(shares, virtualEstate);

            return shares;
        }

        // --- DAĞITIM MANTIKLARI ---

        private void DistributeFirstDegree(decimal estateToDistribute, Calculation input, List<Share> shares)
        {
            try
            {
                var grandchildrenCounts = ParseIntList(input.PredeceasedChildrenGrandchildrenCounts);
                int totalStems = input.LivingChildrenCount + grandchildrenCounts.Count;

                if (totalStems == 0) return;

                decimal sharePerStem = estateToDistribute / totalStems;

                // Yaşayan çocuklar
                for (int i = 1; i <= input.LivingChildrenCount; i++)
                {
                    shares.Add(new Share
                    {
                        HeirType = HeirType.Child,
                        HeirDisplay = $"Çocuk {i}",
                        Amount = sharePerStem,
                        TheoreticalAmount = sharePerStem
                    });
                }

                // Torunlar
                for (int i = 0; i < grandchildrenCounts.Count; i++)
                {
                    int grandchildCount = grandchildrenCounts[i];
                    if (grandchildCount > 0)
                    {
                        decimal sharePerGrandchild = sharePerStem / grandchildCount;
                        for (int j = 1; j <= grandchildCount; j++)
                        {
                            shares.Add(new Share
                            {
                                HeirType = HeirType.Grandchild,
                                HeirDisplay = $"Torun {i + 1}-{j} (Çocuk {input.LivingChildrenCount + i + 1} yerine)",
                                Amount = sharePerGrandchild,
                                TheoreticalAmount = sharePerGrandchild
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"1. Zümre Hatası: {ex.Message}");
            }
        }

        private void DistributeSecondDegree(decimal estateToDistribute, Calculation input, List<Share> shares)
        {
            // 2. Zümre: Baba Kolu ve Anne Kolu
            var paternalStem = new SecondOrderStem
            {
                Name = "Baba Kolu",
                RootAlive = input.FatherAlive,
                AliveSiblings = input.PaternalSiblingsAliveCount,
                PredeceasedSiblingsChildrenCounts = ParseIntList(input.PaternalPredeceasedSiblingsChildrenCounts)
            };

            var maternalStem = new SecondOrderStem
            {
                Name = "Anne Kolu",
                RootAlive = input.MotherAlive,
                AliveSiblings = input.MaternalSiblingsAliveCount,
                PredeceasedSiblingsChildrenCounts = ParseIntList(input.MaternalPredeceasedSiblingsChildrenCounts)
            };

            bool hasPaternal = paternalStem.RootAlive || paternalStem.HasDescendants();
            bool hasMaternal = maternalStem.RootAlive || maternalStem.HasDescendants();

            // KURAL: Bir taraf tamamen boşsa, payı diğer tarafa geçer. İkisi de varsa 1/2 - 1/2 paylaşırlar.
            if (hasPaternal && hasMaternal)
            {
                decimal halfShare = estateToDistribute / 2;
                DistributeSecondOrderSide(paternalStem, halfShare, shares);
                DistributeSecondOrderSide(maternalStem, halfShare, shares);
            }
            else if (hasPaternal)
            {
                DistributeSecondOrderSide(paternalStem, estateToDistribute, shares);
            }
            else if (hasMaternal)
            {
                DistributeSecondOrderSide(maternalStem, estateToDistribute, shares);
            }
        }

        private void DistributeSecondOrderSide(SecondOrderStem stem, decimal amount, List<Share> shares)
        {
            if (stem.RootAlive)
            {
                string displayName = stem.Name == "Baba Kolu" ? "Baba" : "Anne";
                shares.Add(new Share { HeirType = HeirType.Parent, HeirDisplay = displayName, Amount = amount, TheoreticalAmount = amount });
            }
            else
            {
                DistributeSecondOrderStemToDescendants(stem, amount, shares);
            }
        }

        private void DistributeSecondOrderStemToDescendants(SecondOrderStem stem, decimal stemShare, List<Share> shares)
        {
            var descendants = new List<DescendantInfo>();
            for (int i = 1; i <= stem.AliveSiblings; i++) descendants.Add(new DescendantInfo { Type = "kardeş", Index = i });

            int siblingGroupIndex = 1;
            foreach (var nephewCount in stem.PredeceasedSiblingsChildrenCounts)
            {
                if (nephewCount > 0)
                {
                    for (int i = 1; i <= nephewCount; i++) descendants.Add(new DescendantInfo { Type = "yeğen", GroupIndex = siblingGroupIndex, Index = i });
                    siblingGroupIndex++;
                }
            }

            if (descendants.Count == 0) return;
            decimal sharePerDescendant = stemShare / descendants.Count;
            string prefix = stem.Name == "Baba Kolu" ? "Baba tarafı" : "Anne tarafı";

            foreach (var desc in descendants)
            {
                string displayName = desc.Type == "kardeş" ? $"{prefix} {desc.Type} {desc.Index}" : $"{prefix} {desc.Type} {desc.GroupIndex}-{desc.Index}";
                shares.Add(new Share { HeirType = HeirType.Other, HeirDisplay = displayName, Amount = sharePerDescendant, TheoreticalAmount = sharePerDescendant });
            }
        }

        private void DistributeThirdDegree(decimal estateToDistribute, Calculation input, List<Share> shares)
        {
            // 3. ZÜMRE DÜZELTİLMİŞ MANTIK: Önce Baba ve Anne tarafı olarak ikiye ayır

            // Baba Tarafı Kökleri
            var patGrandpa = new ThirdDegreeStem { Name = "Baba tarafı büyükbaba", IsAlive = input.PaternalGrandfatherAlive, UnclesAuntsAliveCount = ParseInt(input.PaternalGrandfatherUnclesAuntsAliveCount), PredeceasedChildrenCounts = ParseIntList(input.PaternalGrandfatherPredeceasedUnclesAuntsChildrenCounts) };
            var patGrandma = new ThirdDegreeStem { Name = "Baba tarafı büyükanne", IsAlive = input.PaternalGrandmotherAlive, UnclesAuntsAliveCount = ParseInt(input.PaternalGrandmotherUnclesAuntsAliveCount), PredeceasedChildrenCounts = ParseIntList(input.PaternalGrandmotherPredeceasedUnclesAuntsChildrenCounts) };

            // Anne Tarafı Kökleri
            var matGrandpa = new ThirdDegreeStem { Name = "Anne tarafı büyükbaba", IsAlive = input.MaternalGrandfatherAlive, UnclesAuntsAliveCount = ParseInt(input.MaternalGrandfatherUnclesAuntsAliveCount), PredeceasedChildrenCounts = ParseIntList(input.MaternalGrandfatherPredeceasedUnclesAuntsChildrenCounts) };
            var matGrandma = new ThirdDegreeStem { Name = "Anne tarafı büyükanne", IsAlive = input.MaternalGrandmotherAlive, UnclesAuntsAliveCount = ParseInt(input.MaternalGrandmotherUnclesAuntsAliveCount), PredeceasedChildrenCounts = ParseIntList(input.MaternalGrandmotherPredeceasedUnclesAuntsChildrenCounts) };

            bool hasPaternalSide = (patGrandpa.IsAlive || patGrandpa.HasDescendants()) || (patGrandma.IsAlive || patGrandma.HasDescendants());
            bool hasMaternalSide = (matGrandpa.IsAlive || matGrandpa.HasDescendants()) || (matGrandma.IsAlive || matGrandma.HasDescendants());

            if (hasPaternalSide && hasMaternalSide)
            {
                // Her iki taraf da var: YARI YARIYA BÖL
                decimal halfShare = estateToDistribute / 2;
                DistributeThirdDegreeSide(halfShare, patGrandpa, patGrandma, shares);
                DistributeThirdDegreeSide(halfShare, matGrandpa, matGrandma, shares);
            }
            else if (hasPaternalSide)
            {
                // Sadece Baba tarafı var: TAMAMINI ALIR
                DistributeThirdDegreeSide(estateToDistribute, patGrandpa, patGrandma, shares);
            }
            else if (hasMaternalSide)
            {
                // Sadece Anne tarafı var: TAMAMINI ALIR
                DistributeThirdDegreeSide(estateToDistribute, matGrandpa, matGrandma, shares);
            }
        }

        private void DistributeThirdDegreeSide(decimal sideShare, ThirdDegreeStem root1, ThirdDegreeStem root2, List<Share> shares)
        {
            // Taraf içindeki dağılım (Dede vs Nine)
            bool hasRoot1 = root1.IsAlive || root1.HasDescendants();
            bool hasRoot2 = root2.IsAlive || root2.HasDescendants();

            if (hasRoot1 && hasRoot2)
            {
                decimal quarterShare = sideShare / 2;
                DistributeThirdDegreeRoot(quarterShare, root1, shares);
                DistributeThirdDegreeRoot(quarterShare, root2, shares);
            }
            else if (hasRoot1)
            {
                DistributeThirdDegreeRoot(sideShare, root1, shares);
            }
            else if (hasRoot2)
            {
                DistributeThirdDegreeRoot(sideShare, root2, shares);
            }
        }

        private void DistributeThirdDegreeRoot(decimal amount, ThirdDegreeStem root, List<Share> shares)
        {
            if (root.IsAlive)
            {
                shares.Add(new Share { HeirType = HeirType.Other, HeirDisplay = root.Name, Amount = amount, TheoreticalAmount = amount });
            }
            else
            {
                DistributeStemToDescendants(root, amount, shares);
            }
        }

        private void DistributeStemToDescendants(ThirdDegreeStem stem, decimal stemShare, List<Share> shares)
        {
            var descendants = new List<DescendantInfo>();
            for (int i = 1; i <= stem.UnclesAuntsAliveCount; i++) descendants.Add(new DescendantInfo { Type = "amca/hala/dayı/teyze", Index = i });

            int cousinGroupIndex = 1;
            foreach (var cousinCount in stem.PredeceasedChildrenCounts)
            {
                if (cousinCount > 0)
                {
                    for (int i = 1; i <= cousinCount; i++) descendants.Add(new DescendantInfo { Type = "kuzen", GroupIndex = cousinGroupIndex, Index = i });
                    cousinGroupIndex++;
                }
            }

            if (descendants.Count == 0) return;
            decimal sharePerDescendant = stemShare / descendants.Count;

            foreach (var desc in descendants)
            {
                string displayName = desc.Type == "amca/hala/dayı/teyze"
                    ? $"{stem.Name} tarafı {desc.Type} {desc.Index}"
                    : $"{stem.Name} tarafı {desc.Type} {desc.GroupIndex}-{desc.Index}";
                shares.Add(new Share { HeirType = HeirType.Other, HeirDisplay = displayName, Amount = sharePerDescendant, TheoreticalAmount = sharePerDescendant });
            }
        }

        // --- YARDIMCI METOTLAR ---

        private void CalculateReservedPortions(List<Share> shares, bool hasFirstDegree, bool hasSecondDegree)
        {
            foreach (var share in shares)
            {
                decimal reservedRate = 0;
                // Saklı Pay oranları (TheoreticalAmount üzerinden hesaplanır)
                switch (share.HeirType)
                {
                    case HeirType.Child:
                    case HeirType.Grandchild:
                        reservedRate = 0.50m; // Altsoy: 1/2
                        break;
                    case HeirType.Parent:
                        reservedRate = 0.25m; // Ana-Baba: 1/4
                        break;
                    case HeirType.Spouse:
                        // Eş: 1. veya 2. zümreyle 1/1, diğerleriyle 3/4
                        reservedRate = (hasFirstDegree || hasSecondDegree) ? 1.00m : 0.75m;
                        break;
                    default:
                        reservedRate = 0; // Kardeşlerin ve diğerlerinin saklı payı yoktur
                        break;
                }

                share.ReservedPortion = share.TheoreticalAmount * reservedRate;

                // İhlal Kontrolü: Eğer (Net Alacağı) < (Saklı Payı) ise ihlal vardır.
                if (share.Amount < share.ReservedPortion)
                {
                    share.IsReservedPortionViolated = true;
                    share.ViolationAmount = share.ReservedPortion - share.Amount;
                }
            }
        }

        private List<AssistanceItem> GetHotchpotItems(Calculation input)
        {
            if (!input.HasAssistance || string.IsNullOrEmpty(input.AssistanceList)) return new List<AssistanceItem>();
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var allItems = JsonSerializer.Deserialize<List<AssistanceItem>>(input.AssistanceList, options) ?? new List<AssistanceItem>();
                return allItems.Where(item => item.CountAsInheritance == "Evet" || item.CountAsInheritance == "Bilmiyorum").ToList();
            }
            catch { return new List<AssistanceItem>(); }
        }

        private void ApplyDeductions(List<Share> shares, List<AssistanceItem> assistanceItems)
        {
            if (assistanceItems.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("=== DENKLEŞTİRME MAHSUP ===");
                System.Diagnostics.Debug.WriteLine("Denkleştirme item'ı yok, mahsup yapılmıyor");
                return;
            }
            
            System.Diagnostics.Debug.WriteLine($"=== DENKLEŞTİRME MAHSUP ===");
            System.Diagnostics.Debug.WriteLine($"Mahsup yapılacak item sayısı: {assistanceItems.Count}");
            System.Diagnostics.Debug.WriteLine($"Mevcut pay sayısı: {shares.Count}");
            
            // Debug: Tüm assistance item'larını listele
            foreach (var item in assistanceItems)
            {
                System.Diagnostics.Debug.WriteLine($"  Assistance Item: '{item.HeirName}' - {item.EstimatedValue:N2} TL");
            }
            
            // Debug: Tüm share'leri listele
            foreach (var share in shares)
            {
                System.Diagnostics.Debug.WriteLine($"  Share: '{share.HeirDisplay}' - {share.Amount:N2} TL");
            }
            
            foreach (var share in shares)
            {
                // Daha esnek eşleştirme: Trim, case-insensitive ve boşluk normalizasyonu
                var shareNameNormalized = share.HeirDisplay.Trim().Replace("  ", " ").ToLowerInvariant();
                
                var receivedGifts = assistanceItems
                    .Where(a => 
                    {
                        var heirNameNormalized = a.HeirName.Trim().Replace("  ", " ").ToLowerInvariant();
                        bool matches = heirNameNormalized == shareNameNormalized;
                        if (!matches)
                        {
                            System.Diagnostics.Debug.WriteLine($"    Eşleşme kontrolü: '{a.HeirName}' != '{share.HeirDisplay}'");
                        }
                        return matches;
                    })
                    .Sum(a => a.EstimatedValue);

                if (receivedGifts > 0)
                {
                    decimal oldAmount = share.Amount;
                    // Mahsup işlemi: Hakediş - Hediye
                    share.Amount = Math.Max(0, share.Amount - receivedGifts);
                    System.Diagnostics.Debug.WriteLine($"  ✓ {share.HeirDisplay}: {oldAmount:N2} TL -> {share.Amount:N2} TL (Mahsup: {receivedGifts:N2} TL)");
                }
            }
        }

        private bool HasFirstDegreeHeirs(Calculation input) => input.LivingChildrenCount > 0 || (input.HasPredeceasedChild && input.HasGrandchildrenFromPredeceasedChild);
        private bool HasSecondDegreeHeirs(Calculation input) => input.FatherAlive || input.MotherAlive || input.PaternalSiblingsAliveCount > 0 || input.MaternalSiblingsAliveCount > 0 || HasPredeceasedSiblingsChildren(input.PaternalPredeceasedSiblingsChildrenCounts) || HasPredeceasedSiblingsChildren(input.MaternalPredeceasedSiblingsChildrenCounts);
        private bool HasThirdDegreeHeirs(Calculation input) { bool hasGrandparents = input.PaternalGrandfatherAlive || input.PaternalGrandmotherAlive || input.MaternalGrandfatherAlive || input.MaternalGrandmotherAlive; return hasGrandparents || HasUnclesAuntsAlive(input) || HasPredeceasedUnclesAuntsChildren(input); }
        private bool HasPredeceasedSiblingsChildren(string jsonCounts) => ParseIntList(jsonCounts).Any(c => c > 0);
        private bool HasUnclesAuntsAlive(Calculation input) => ParseInt(input.PaternalGrandfatherUnclesAuntsAliveCount) > 0 || ParseInt(input.PaternalGrandmotherUnclesAuntsAliveCount) > 0 || ParseInt(input.MaternalGrandfatherUnclesAuntsAliveCount) > 0 || ParseInt(input.MaternalGrandmotherUnclesAuntsAliveCount) > 0;
        private bool HasPredeceasedUnclesAuntsChildren(Calculation input) => HasPredeceasedSiblingsChildren(input.PaternalGrandfatherPredeceasedUnclesAuntsChildrenCounts) || HasPredeceasedSiblingsChildren(input.PaternalGrandmotherPredeceasedUnclesAuntsChildrenCounts) || HasPredeceasedSiblingsChildren(input.MaternalGrandfatherPredeceasedUnclesAuntsChildrenCounts) || HasPredeceasedSiblingsChildren(input.MaternalGrandmotherPredeceasedUnclesAuntsChildrenCounts);
        private int ParseInt(string value) => int.TryParse(value, out int result) ? result : 0;
        private List<int> ParseIntList(string json) { try { return JsonSerializer.Deserialize<List<int>>(json ?? "[]") ?? new List<int>(); } catch { return new List<int>(); } }

        private void NormalizeFractions(List<Share> shares, decimal baseAmount)
        {
            foreach (var share in shares)
            {
                if (baseAmount > 0)
                {
                    var fraction = share.Amount / baseAmount;
                    share.FractionNumerator = Math.Min((int)(fraction * 1000), int.MaxValue);
                    share.FractionDenominator = 1000;
                }
            }
        }

        // --- HELPER CLASSES ---
        private class ThirdDegreeStem { public string Name { get; set; } = ""; public bool IsAlive { get; set; } public int UnclesAuntsAliveCount { get; set; } public List<int> PredeceasedChildrenCounts { get; set; } = new(); public bool HasDescendants() => UnclesAuntsAliveCount > 0 || PredeceasedChildrenCounts.Any(c => c > 0); }
        private class SecondOrderStem { public string Name { get; set; } = ""; public bool RootAlive { get; set; } public int AliveSiblings { get; set; } public List<int> PredeceasedSiblingsChildrenCounts { get; set; } = new(); public bool HasDescendants() => AliveSiblings > 0 || PredeceasedSiblingsChildrenCounts.Any(c => c > 0); }
        private class DescendantInfo { public string Type { get; set; } = ""; public int Index { get; set; } public int GroupIndex { get; set; } }
    }
}