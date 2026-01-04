using System;
using System.Collections.Generic;

namespace MirasPaylasim.Models
{
    public enum SpouseStatus
    {
        Alive = 1,
        NotAlive = 2
    }

    public enum SpouseDeathTiming
    {
        NotApplicable = 0,
        BeforeDeceased = 1,
        AfterDeceased = 2
    }

    public enum ParentsAlive
    {
        None = 0,
        MotherOnly = 1,
        FatherOnly = 2,
        Both = 3
    }

    public class Calculation
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Kullanıcı ilişkisi
        public string? UserId { get; set; }
        public virtual ApplicationUser? User { get; set; }

        // Estate inputs
        public decimal TotalAssets { get; set; }
        public decimal Receivables { get; set; }
        public decimal Debts { get; set; }

        public decimal NetEstate
        {
            get
            {
                decimal net = TotalAssets + Receivables - Debts;
                
                System.Diagnostics.Debug.WriteLine($"=== NETESTATE HESAPLAMA ===");
                System.Diagnostics.Debug.WriteLine($"TotalAssets: {TotalAssets}");
                System.Diagnostics.Debug.WriteLine($"Receivables: {Receivables}");
                System.Diagnostics.Debug.WriteLine($"Debts: {Debts}");
                System.Diagnostics.Debug.WriteLine($"İlk net: {net}");
                System.Diagnostics.Debug.WriteLine($"UnfairlyTakenList: {UnfairlyTakenList}");
                
                // NOT: Denkleştirme yardımları NetEstate'e eklenmez
                // Denkleştirme, hesaplama sırasında her seviye için ayrı ayrı uygulanır
                
                // Terekenin İadesi: Haksız alınmış malları ekle (tüm mirasçılara etki eder)
                if (!string.IsNullOrEmpty(UnfairlyTakenList))
                {
                    try
                    {
                        var options = new System.Text.Json.JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        };
                        var unfairlyTakenItems = System.Text.Json.JsonSerializer.Deserialize<List<UnfairlyTakenItem>>(UnfairlyTakenList, options) ?? new List<UnfairlyTakenItem>();
                        System.Diagnostics.Debug.WriteLine($"UnfairlyTakenItems sayısı: {unfairlyTakenItems.Count}");
                        foreach (var item in unfairlyTakenItems)
                        {
                            System.Diagnostics.Debug.WriteLine($"UnfairlyTakenItem: {item.PropertyType}, Değer: {item.EstimatedValue}");
                            net += item.EstimatedValue;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"UnfairlyTakenList deserialize hatası: {ex.Message}");
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"Son net: {net}");
                return net;
            }
        }

        // Spouse
        public SpouseStatus SpouseStatus { get; set; }
        public SpouseDeathTiming SpouseDeathTiming { get; set; } = SpouseDeathTiming.NotApplicable;

        // Children / descendants
        public int LivingChildrenCount { get; set; }
        public bool HasPredeceasedChild { get; set; }
        public bool HasGrandchildrenFromPredeceasedChild { get; set; }
        public string? PredeceasedChildNotes { get; set; }
        public string PredeceasedChildrenGrandchildrenCounts { get; set; } = "[]"; // JSON array

        // Parents
        public ParentsAlive ParentsAlive { get; set; }

        // 2. Zümre - Anne/Baba kolları
        public bool FatherAlive { get; set; }
        public bool MotherAlive { get; set; }

        // Baba kolu
        public int PaternalSiblingsAliveCount { get; set; }
        public string PaternalPredeceasedSiblingsChildrenCounts { get; set; } = "[]"; // JSON array

        // Anne kolu  
        public int MaternalSiblingsAliveCount { get; set; }
        public string MaternalPredeceasedSiblingsChildrenCounts { get; set; } = "[]"; // JSON array

        // 3. Zümre - Dört kök
        public bool PaternalGrandfatherAlive { get; set; }
        public bool PaternalGrandmotherAlive { get; set; }
        public bool MaternalGrandfatherAlive { get; set; }
        public bool MaternalGrandmotherAlive { get; set; }

        // Her kök için amca/hala/dayı/teyze bilgileri
        public string PaternalGrandfatherUnclesAuntsAliveCount { get; set; } = "0";
        public string PaternalGrandfatherPredeceasedUnclesAuntsChildrenCounts { get; set; } = "[]";

        public string PaternalGrandmotherUnclesAuntsAliveCount { get; set; } = "0";
        public string PaternalGrandmotherPredeceasedUnclesAuntsChildrenCounts { get; set; } = "[]";

        public string MaternalGrandfatherUnclesAuntsAliveCount { get; set; } = "0";
        public string MaternalGrandfatherPredeceasedUnclesAuntsChildrenCounts { get; set; } = "[]";

        public string MaternalGrandmotherUnclesAuntsAliveCount { get; set; } = "0";
        public string MaternalGrandmotherPredeceasedUnclesAuntsChildrenCounts { get; set; } = "[]";

        // Consent
        public bool ConsentGiven { get; set; }

        // Denkleştirme (Hotchpot)
        public bool HasAssistance { get; set; }
        public string AssistanceList { get; set; } = "[]"; // JSON array of AssistanceItem

        // Terekenin İadesi (Return of Estate)
        public bool HasUnfairlyTaken { get; set; }
        public string UnfairlyTakenList { get; set; } = "[]"; // JSON array of UnfairlyTakenItem

        public List<Share> Shares { get; set; } = new();
    }

    public class AssistanceItem
    {
        public string HeirName { get; set; } = "";
        public string AssistanceType { get; set; } = ""; // Ev, Araba, Para, Arsa, İş Kurma Desteği, Diğer
        public decimal EstimatedValue { get; set; }
        public string CountAsInheritance { get; set; } = ""; // Evet, Hayır, Bilmiyorum
    }

    public class UnfairlyTakenItem
    {
        public string PropertyType { get; set; } = ""; // Ev, Arsa, Araç, Para, Banka hesabı, Değerli eşya, Diğer
        public decimal EstimatedValue { get; set; }
        public string TakenBy { get; set; } = ""; // Mirasçı, Üçüncü kişi
        public string Method { get; set; } = ""; // Hile, Baskı, Gizleme, Habersiz çekme, Usulsüz devir, Diğer
    }
}