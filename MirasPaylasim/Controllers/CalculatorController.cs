using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using MirasPaylasim.Data;
using MirasPaylasim.Models;
using MirasPaylasim.Services;

namespace MirasPaylasim.Controllers
{
    public class CalculatorController : Controller
    {
        private readonly MirasContext _db;
        private readonly IInheritanceCalculator _calculator;

        public CalculatorController(MirasContext db, IInheritanceCalculator calculator)
        {
            _db = db;
            _calculator = calculator;
        }

        [HttpGet("calculator/start")]
        [Authorize]
        public IActionResult Start()
        {
            return View();
        }

        [HttpPost("calculator/start")]
        [Authorize]
        public IActionResult StartPost(bool consent)
        {
            if (!consent)
            {
                ModelState.AddModelError("consent", "Devam etmek için KVKK onayı gereklidir.");
                return View("Start");
            }
            TempData["consent"] = true;
            return RedirectToAction("Spouse");
        }

        [HttpGet("calculator/estate")]
        public IActionResult Estate()
        {
            var calc = GetCalcFromTemp();
            if (calc == null)
            {
                return RedirectToAction("Start");
            }
            return View();
        }

        [HttpPost("calculator/estate")]
        public IActionResult EstatePost(
            decimal totalAssets, 
            decimal rentReceivable, 
            decimal receivables, 
            decimal companyShares, 
            decimal insuranceCompensation)
        {
            var calc = GetCalcFromTemp();
            if (calc == null)
            {
                return RedirectToAction("Start");
            }
            
            // Malvarlığı toplamı (artıran değerler)
            calc.TotalAssets = totalAssets + rentReceivable + companyShares + insuranceCompensation;
            
            // Alacaklar
            calc.Receivables = receivables;
            
            calc.ConsentGiven = (bool?)TempData["consent"] == true;
            TempData["calc"] = System.Text.Json.JsonSerializer.Serialize(calc);
            return RedirectToAction("Debts");
        }

        [HttpGet("calculator/debts")]
        public IActionResult Debts()
        {
            var calc = GetCalcFromTemp();
            if (calc == null)
            {
                return RedirectToAction("Start");
            }
            return View();
        }

        [HttpPost("calculator/debts")]
        public IActionResult DebtsPost(
            decimal funeralExpenses,
            decimal treatmentExpenses,
            decimal bankLoans,
            decimal taxDebts,
            decimal ssiDebts,
            decimal rentDebts,
            decimal commercialDebts,
            decimal alimonyDebts,
            decimal willDebts,
            decimal businessDebts)
        {
            var calc = GetCalcFromTemp();
            if (calc == null)
            {
                return RedirectToAction("Start");
            }
            
            // Borçlar toplamı (azaltan değerler)
            calc.Debts = funeralExpenses + treatmentExpenses + bankLoans + taxDebts + 
                        ssiDebts + rentDebts + commercialDebts + alimonyDebts + 
                        willDebts + businessDebts;
            
            TempData["calc"] = System.Text.Json.JsonSerializer.Serialize(calc);
            return RedirectToAction("Assistance");
        }

        [HttpGet("calculator/assistance")]
        public IActionResult Assistance()
        {
            var calc = GetCalcFromTemp();
            if (calc == null)
            {
                return RedirectToAction("Start");
            }
            return View();
        }

        [HttpPost("calculator/assistance")]
        public IActionResult AssistancePost(bool hasAssistance)
        {
            var calc = GetCalcFromTemp();
            if (calc == null)
            {
                return RedirectToAction("Start");
            }
            
            calc.HasAssistance = hasAssistance;
            
            // Eğer hasAssistance false ise, AssistanceList'i temizle
            if (!hasAssistance)
            {
                calc.AssistanceList = "[]";
            }
            
            TempData["calc"] = System.Text.Json.JsonSerializer.Serialize(calc);
            
            if (hasAssistance)
            {
                return RedirectToAction("AssistanceForm");
            }
            else
            {
                return RedirectToAction("UnfairlyTaken");
            }
        }

        [HttpGet("calculator/assistance-form")]
        public IActionResult AssistanceForm()
        {
            var calc = GetCalcFromTemp();
            if (calc == null)
            {
                return RedirectToAction("Start");
            }
            
            // Potansiyel mirasçı listesini oluştur
            var potentialHeirs = GetPotentialHeirs(calc);
            ViewBag.PotentialHeirs = potentialHeirs;
            
            return View();
        }
        
        private List<string> GetPotentialHeirs(Calculation calc)
        {
            var heirs = new List<string>();
            
            // Eş
            bool spouseAlive = calc.SpouseStatus == SpouseStatus.Alive || 
                              (calc.SpouseStatus == SpouseStatus.NotAlive && calc.SpouseDeathTiming == SpouseDeathTiming.AfterDeceased);
            if (spouseAlive)
            {
                heirs.Add("Eş");
            }
            
            // Çocuklar
            for (int i = 1; i <= calc.LivingChildrenCount; i++)
            {
                heirs.Add($"Çocuk {i}");
            }
            
            // Torunlar
            try
            {
                var grandchildrenCounts = System.Text.Json.JsonSerializer.Deserialize<List<int>>(calc.PredeceasedChildrenGrandchildrenCounts ?? "[]") ?? new List<int>();
                for (int i = 0; i < grandchildrenCounts.Count; i++)
                {
                    int grandchildCount = grandchildrenCounts[i];
                    for (int j = 1; j <= grandchildCount; j++)
                    {
                        heirs.Add($"Torun {i + 1}-{j} (Çocuk {calc.LivingChildrenCount + i + 1} yerine)");
                    }
                }
            }
            catch { }
            
            // Anne/Baba
            if (calc.FatherAlive)
            {
                heirs.Add("Baba");
            }
            if (calc.MotherAlive)
            {
                heirs.Add("Anne");
            }
            
            // Kardeşler (2. Zümre)
            string prefix = "Baba tarafı";
            for (int i = 1; i <= calc.PaternalSiblingsAliveCount; i++)
            {
                heirs.Add($"{prefix} kardeş {i}");
            }
            try
            {
                var paternalNephewCounts = System.Text.Json.JsonSerializer.Deserialize<List<int>>(calc.PaternalPredeceasedSiblingsChildrenCounts ?? "[]") ?? new List<int>();
                for (int i = 0; i < paternalNephewCounts.Count; i++)
                {
                    int nephewCount = paternalNephewCounts[i];
                    for (int j = 1; j <= nephewCount; j++)
                    {
                        heirs.Add($"{prefix} yeğen {i + 1}-{j}");
                    }
                }
            }
            catch { }
            
            prefix = "Anne tarafı";
            for (int i = 1; i <= calc.MaternalSiblingsAliveCount; i++)
            {
                heirs.Add($"{prefix} kardeş {i}");
            }
            try
            {
                var maternalNephewCounts = System.Text.Json.JsonSerializer.Deserialize<List<int>>(calc.MaternalPredeceasedSiblingsChildrenCounts ?? "[]") ?? new List<int>();
                for (int i = 0; i < maternalNephewCounts.Count; i++)
                {
                    int nephewCount = maternalNephewCounts[i];
                    for (int j = 1; j <= nephewCount; j++)
                    {
                        heirs.Add($"{prefix} yeğen {i + 1}-{j}");
                    }
                }
            }
            catch { }
            
            // 3. Zümre - Büyükanne/büyükbaba
            if (calc.PaternalGrandfatherAlive)
            {
                heirs.Add("Baba tarafı büyükbaba");
            }
            if (calc.PaternalGrandmotherAlive)
            {
                heirs.Add("Baba tarafı büyükanne");
            }
            if (calc.MaternalGrandfatherAlive)
            {
                heirs.Add("Anne tarafı büyükbaba");
            }
            if (calc.MaternalGrandmotherAlive)
            {
                heirs.Add("Anne tarafı büyükanne");
            }
            
            // Amca/hala/dayı/teyze ve kuzenler
            AddThirdDegreeHeirs(calc, "Baba tarafı büyükbaba", calc.PaternalGrandfatherUnclesAuntsAliveCount, calc.PaternalGrandfatherPredeceasedUnclesAuntsChildrenCounts, heirs);
            AddThirdDegreeHeirs(calc, "Baba tarafı büyükanne", calc.PaternalGrandmotherUnclesAuntsAliveCount, calc.PaternalGrandmotherPredeceasedUnclesAuntsChildrenCounts, heirs);
            AddThirdDegreeHeirs(calc, "Anne tarafı büyükbaba", calc.MaternalGrandfatherUnclesAuntsAliveCount, calc.MaternalGrandfatherPredeceasedUnclesAuntsChildrenCounts, heirs);
            AddThirdDegreeHeirs(calc, "Anne tarafı büyükanne", calc.MaternalGrandmotherUnclesAuntsAliveCount, calc.MaternalGrandmotherPredeceasedUnclesAuntsChildrenCounts, heirs);
            
            return heirs;
        }
        
        private void AddThirdDegreeHeirs(Calculation calc, string grandparentName, string unclesAuntsCount, string cousinsCounts, List<string> heirs)
        {
            int count = 0;
            if (int.TryParse(unclesAuntsCount, out count))
            {
                for (int i = 1; i <= count; i++)
                {
                    heirs.Add($"{grandparentName} tarafı amca/hala/dayı/teyze {i}");
                }
            }
            
            try
            {
                var cousinCounts = System.Text.Json.JsonSerializer.Deserialize<List<int>>(cousinsCounts ?? "[]") ?? new List<int>();
                for (int i = 0; i < cousinCounts.Count; i++)
                {
                    int cousinCount = cousinCounts[i];
                    for (int j = 1; j <= cousinCount; j++)
                    {
                        heirs.Add($"{grandparentName} tarafı kuzen {i + 1}-{j}");
                    }
                }
            }
            catch { }
        }

        [HttpPost("calculator/assistance-form")]
        public IActionResult AssistanceFormPost(string assistanceList)
        {
            var calc = GetCalcFromTemp();
            if (calc == null)
            {
                return RedirectToAction("Start");
            }
            
            System.Diagnostics.Debug.WriteLine($"=== ASSISTANCE FORM POST DEBUG ===");
            System.Diagnostics.Debug.WriteLine($"Gelen assistanceList: '{assistanceList}'");
            
            // Boş string veya null ise boş array olarak kaydet
            if (string.IsNullOrWhiteSpace(assistanceList))
            {
                calc.AssistanceList = "[]";
            }
            else
            {
                calc.AssistanceList = assistanceList;
            }
            calc.HasAssistance = true; // Form doldurulduğu için true yap
            
            System.Diagnostics.Debug.WriteLine($"Kaydedilen AssistanceList: '{calc.AssistanceList}'");
            System.Diagnostics.Debug.WriteLine($"HasAssistance: {calc.HasAssistance}");
            
            TempData["calc"] = System.Text.Json.JsonSerializer.Serialize(calc);
            return RedirectToAction("UnfairlyTaken");
        }

        [HttpGet("calculator/unfairly-taken")]
        public IActionResult UnfairlyTaken()
        {
            var calc = GetCalcFromTemp();
            if (calc == null)
            {
                return RedirectToAction("Start");
            }
            return View();
        }

        [HttpPost("calculator/unfairly-taken")]
        public IActionResult UnfairlyTakenPost(bool hasUnfairlyTaken)
        {
            var calc = GetCalcFromTemp();
            if (calc == null)
            {
                return RedirectToAction("Start");
            }
            
            calc.HasUnfairlyTaken = hasUnfairlyTaken;
            
            // Eğer hasUnfairlyTaken false ise, UnfairlyTakenList'i temizle
            if (!hasUnfairlyTaken)
            {
                calc.UnfairlyTakenList = "[]";
            }
            
            TempData["calc"] = System.Text.Json.JsonSerializer.Serialize(calc);
            
            if (hasUnfairlyTaken)
            {
                return RedirectToAction("UnfairlyTakenForm");
            }
            else
            {
                return RedirectToAction("Compute");
            }
        }

        [HttpGet("calculator/unfairly-taken-form")]
        public IActionResult UnfairlyTakenForm()
        {
            var calc = GetCalcFromTemp();
            if (calc == null)
            {
                return RedirectToAction("Start");
            }
            
            // Potansiyel mirasçı listesini oluştur
            var potentialHeirs = GetPotentialHeirs(calc);
            ViewBag.PotentialHeirs = potentialHeirs;
            
            return View();
        }

        [HttpPost("calculator/unfairly-taken-form")]
        public IActionResult UnfairlyTakenFormPost(string unfairlyTakenList)
        {
            var calc = GetCalcFromTemp();
            if (calc == null)
            {
                return RedirectToAction("Start");
            }
            
            System.Diagnostics.Debug.WriteLine($"=== UNFAIRLY TAKEN FORM POST ===");
            System.Diagnostics.Debug.WriteLine($"Gelen unfairlyTakenList: '{unfairlyTakenList}'");
            
            // Boş string veya null ise boş array olarak kaydet
            if (string.IsNullOrWhiteSpace(unfairlyTakenList))
            {
                calc.UnfairlyTakenList = "[]";
            }
            else
            {
                calc.UnfairlyTakenList = unfairlyTakenList;
            }
            
            System.Diagnostics.Debug.WriteLine($"Kaydedilen UnfairlyTakenList: '{calc.UnfairlyTakenList}'");
            
            TempData["calc"] = System.Text.Json.JsonSerializer.Serialize(calc);
            return RedirectToAction("Compute");
        }

        private Calculation? GetCalcFromTemp()
        {
            if (TempData["calc"] is string s)
            {
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var c = System.Text.Json.JsonSerializer.Deserialize<Calculation>(s, options);
                // keep for next step
                TempData["calc"] = s;
                return c;
            }
            return null;
        }

        [HttpGet("calculator/spouse")]
        public IActionResult Spouse()
        {
            return View();
        }

        [HttpPost("calculator/spouse")]
        public IActionResult SpousePost(SpouseStatus spouseStatus, SpouseDeathTiming spouseDeathTiming)
        {
            var calc = GetCalcFromTemp() ?? new Calculation();
            calc.SpouseStatus = spouseStatus;
            
            // Eğer eş sağ ise, vefat zamanı uygulanmaz
            if (spouseStatus == SpouseStatus.Alive)
            {
                calc.SpouseDeathTiming = SpouseDeathTiming.NotApplicable;
            }
            else
            {
                calc.SpouseDeathTiming = spouseDeathTiming;
            }
            
            TempData["calc"] = System.Text.Json.JsonSerializer.Serialize(calc);
            return RedirectToAction("Children");
        }

        [HttpGet("calculator/children")]
        public IActionResult Children()
        {
            return View();
        }

        [HttpPost("calculator/children")]
        public IActionResult ChildrenPost(int livingChildrenCount, string predeceasedChildrenGrandchildrenCounts)
        {
            var calc = GetCalcFromTemp() ?? new Calculation();
            calc.LivingChildrenCount = livingChildrenCount;
            calc.PredeceasedChildrenGrandchildrenCounts = predeceasedChildrenGrandchildrenCounts ?? "[]";
            
            // JSON'dan ölen çocuk var mı kontrol et
            bool hasPredeceasedChild = false;
            bool hasGrandchildren = false;
            try
            {
                var counts = System.Text.Json.JsonSerializer.Deserialize<List<int>>(predeceasedChildrenGrandchildrenCounts ?? "[]");
                hasPredeceasedChild = counts?.Count > 0;
                hasGrandchildren = counts?.Any(c => c > 0) == true;
            }
            catch { }
            
            calc.HasPredeceasedChild = hasPredeceasedChild;
            calc.HasGrandchildrenFromPredeceasedChild = hasGrandchildren;
            TempData["calc"] = System.Text.Json.JsonSerializer.Serialize(calc);
            
            // 1. Zümre kontrolü - çocuk/torun varsa direkt malvarlığı ekranına
            if (livingChildrenCount > 0 || hasGrandchildren)
            {
                return RedirectToAction("Estate");
            }
            
            // 1. Zümre yoksa anne/baba kontrolüne geç
            return RedirectToAction("Parents");
        }

        [HttpGet("calculator/parents")]
        public IActionResult Parents()
        {
            return View();
        }

        [HttpPost("calculator/parents")]
        public IActionResult ParentsPost(ParentsAlive parentsAlive)
        {
            var calc = GetCalcFromTemp() ?? new Calculation();
            calc.ParentsAlive = parentsAlive;
            
            // ParentsAlive'ı FatherAlive ve MotherAlive'a dönüştür
            calc.FatherAlive = parentsAlive == ParentsAlive.FatherOnly || parentsAlive == ParentsAlive.Both;
            calc.MotherAlive = parentsAlive == ParentsAlive.MotherOnly || parentsAlive == ParentsAlive.Both;
            
            // PRD: Anne ve baba her ikisi de sağsa, SADECE ONLAR mirasçı olur, kardeşler mirasçı değildir
            // Bu durumda kardeş bilgilerini 0 olarak set edip direkt malvarlığı ekranına geç
            if (calc.FatherAlive && calc.MotherAlive)
            {
                calc.PaternalSiblingsAliveCount = 0;
                calc.PaternalPredeceasedSiblingsChildrenCounts = "[]";
                calc.MaternalSiblingsAliveCount = 0;
                calc.MaternalPredeceasedSiblingsChildrenCounts = "[]";
                TempData["calc"] = System.Text.Json.JsonSerializer.Serialize(calc);
                return RedirectToAction("Estate");
            }
            
            // Sadece biri sağsa veya her ikisi de ölüyse, ölen tarafın kardeş bilgilerini sormak için 2. zümre ekranına git
            TempData["calc"] = System.Text.Json.JsonSerializer.Serialize(calc);
            return RedirectToAction("SecondDegree");
        }

        [HttpGet("calculator/second-degree")]
        public IActionResult SecondDegree()
        {
            var calc = GetCalcFromTemp();
            if (calc == null)
            {
                return RedirectToAction("Start");
            }
            
            // View'a anne-baba durumunu gönder (hangi kolonların gösterileceğini belirlemek için)
            ViewBag.FatherAlive = calc.FatherAlive;
            ViewBag.MotherAlive = calc.MotherAlive;
            
            return View();
        }

        [HttpPost("calculator/second-degree")]
        public IActionResult SecondDegreePost(
            int paternalSiblingsAliveCount, string paternalPredeceasedSiblingsChildrenCounts,
            int maternalSiblingsAliveCount, string maternalPredeceasedSiblingsChildrenCounts)
        {
            var calc = GetCalcFromTemp() ?? new Calculation();
            calc.PaternalSiblingsAliveCount = paternalSiblingsAliveCount;
            calc.PaternalPredeceasedSiblingsChildrenCounts = paternalPredeceasedSiblingsChildrenCounts;
            calc.MaternalSiblingsAliveCount = maternalSiblingsAliveCount;
            calc.MaternalPredeceasedSiblingsChildrenCounts = maternalPredeceasedSiblingsChildrenCounts;
            TempData["calc"] = System.Text.Json.JsonSerializer.Serialize(calc);
            
            // PRD: 3. Zümre ekranı sadece 1. ve 2. zümre tamamen boşsa açılır
            // 2. Zümre kontrolü - anne/baba veya kardeş/yeğen varsa hesaplama
            bool hasSecondDegree = calc.FatherAlive || calc.MotherAlive || 
                                   paternalSiblingsAliveCount > 0 || maternalSiblingsAliveCount > 0 ||
                                   HasPredeceasedSiblingsChildren(paternalPredeceasedSiblingsChildrenCounts) ||
                                   HasPredeceasedSiblingsChildren(maternalPredeceasedSiblingsChildrenCounts);
            
            if (hasSecondDegree)
            {
                return RedirectToAction("Estate");
            }
            
            // 2. Zümre de yoksa 3. zümreye geç (1. zümre zaten ChildrenPost'ta kontrol edildi)
            return RedirectToAction("ThirdDegree");
        }

        [HttpGet("calculator/third-degree")]
        public IActionResult ThirdDegree()
        {
            return View();
        }

        [HttpPost("calculator/third-degree")]
        public IActionResult ThirdDegreePost(
            bool paternalGrandfatherAlive, bool paternalGrandmotherAlive,
            bool maternalGrandfatherAlive, bool maternalGrandmotherAlive,
            string paternalGrandfatherUnclesAuntsAliveCount, string paternalGrandfatherPredeceasedUnclesAuntsChildrenCounts,
            string paternalGrandmotherUnclesAuntsAliveCount, string paternalGrandmotherPredeceasedUnclesAuntsChildrenCounts,
            string maternalGrandfatherUnclesAuntsAliveCount, string maternalGrandfatherPredeceasedUnclesAuntsChildrenCounts,
            string maternalGrandmotherUnclesAuntsAliveCount, string maternalGrandmotherPredeceasedUnclesAuntsChildrenCounts)
        {
            var calc = GetCalcFromTemp() ?? new Calculation();
            calc.PaternalGrandfatherAlive = paternalGrandfatherAlive;
            calc.PaternalGrandmotherAlive = paternalGrandmotherAlive;
            calc.MaternalGrandfatherAlive = maternalGrandfatherAlive;
            calc.MaternalGrandmotherAlive = maternalGrandmotherAlive;
            calc.PaternalGrandfatherUnclesAuntsAliveCount = paternalGrandfatherUnclesAuntsAliveCount ?? "0";
            calc.PaternalGrandfatherPredeceasedUnclesAuntsChildrenCounts = paternalGrandfatherPredeceasedUnclesAuntsChildrenCounts ?? "[]";
            calc.PaternalGrandmotherUnclesAuntsAliveCount = paternalGrandmotherUnclesAuntsAliveCount ?? "0";
            calc.PaternalGrandmotherPredeceasedUnclesAuntsChildrenCounts = paternalGrandmotherPredeceasedUnclesAuntsChildrenCounts ?? "[]";
            calc.MaternalGrandfatherUnclesAuntsAliveCount = maternalGrandfatherUnclesAuntsAliveCount ?? "0";
            calc.MaternalGrandfatherPredeceasedUnclesAuntsChildrenCounts = maternalGrandfatherPredeceasedUnclesAuntsChildrenCounts ?? "[]";
            calc.MaternalGrandmotherUnclesAuntsAliveCount = maternalGrandmotherUnclesAuntsAliveCount ?? "0";
            calc.MaternalGrandmotherPredeceasedUnclesAuntsChildrenCounts = maternalGrandmotherPredeceasedUnclesAuntsChildrenCounts ?? "[]";
            
            // ÖNEMLİ: TempData'ya kaydet, yoksa Estate metodunda veriler kaybolur!
            TempData["calc"] = System.Text.Json.JsonSerializer.Serialize(calc);

            // malvarlığı ekranına geç
            return RedirectToAction("Estate");
        }

        [HttpGet("calculator/compute")]
        [HttpPost("calculator/compute")]
        public IActionResult Compute()
        {
            // Giriş kontrolü
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Start") });
            }

            var calc = GetCalcFromTemp();
            if (calc == null)
            {
                return RedirectToAction("Start");
            }

            // Debug: Tüm değerleri kontrol et
            System.Diagnostics.Debug.WriteLine($"=== COMPUTE DEBUG ===");
            System.Diagnostics.Debug.WriteLine($"AssistanceList: {calc.AssistanceList}");
            System.Diagnostics.Debug.WriteLine($"HasAssistance: {calc.HasAssistance}");
            System.Diagnostics.Debug.WriteLine($"UnfairlyTakenList: {calc.UnfairlyTakenList}");
            System.Diagnostics.Debug.WriteLine($"HasUnfairlyTaken: {calc.HasUnfairlyTaken}");
            System.Diagnostics.Debug.WriteLine($"TotalAssets: {calc.TotalAssets}");
            System.Diagnostics.Debug.WriteLine($"Receivables: {calc.Receivables}");
            System.Diagnostics.Debug.WriteLine($"Debts: {calc.Debts}");
            System.Diagnostics.Debug.WriteLine($"NetEstate: {calc.NetEstate}");

            // Kullanıcı ID'sini ekle (AspNetUsers.Id - GUID)
            calc.UserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // compute and save
            var shares = _calculator.Calculate(calc);
            
            // Debug: Hesaplama sonrası kontrol
            System.Diagnostics.Debug.WriteLine($"=== HESAPLAMA SONRASI ===");
            System.Diagnostics.Debug.WriteLine($"Toplam pay sayısı: {shares.Count}");
            foreach (var share in shares)
            {
                System.Diagnostics.Debug.WriteLine($"  {share.HeirDisplay}: {share.Amount:N2} TL");
            }
            calc.Shares = shares;
            _db.Hesaplamalar.Add(calc);
            _db.SaveChanges();
            return RedirectToAction("Result", new { id = calc.Id });
        }

        [HttpGet("calculator/result/{id}")]
        public IActionResult Result(int id)
        {
            var calc = _db.Hesaplamalar.Where(c => c.Id == id).Select(c => new
            {
                c.Id,
                c.TotalAssets,
                c.Receivables,
                c.Debts,
                c.NetEstate,
                Shares = c.Shares.Select(s => new { s.HeirDisplay, s.FractionNumerator, s.FractionDenominator, s.Amount })
            }).FirstOrDefault();

            // PDF için JSON verisi hazırla
            if (calc != null)
            {
                var pdfData = new
                {
                    totalAssets = calc.TotalAssets,
                    receivables = calc.Receivables,
                    debts = calc.Debts,
                    netEstate = calc.NetEstate,
                    shares = calc.Shares.Select(s => new
                    {
                        heirDisplay = s.HeirDisplay,
                        fractionNumerator = s.FractionNumerator,
                        fractionDenominator = s.FractionDenominator,
                        amount = s.Amount
                    }).ToList()
                };
                ViewBag.PdfData = System.Text.Json.JsonSerializer.Serialize(pdfData);
            }

            return View(calc);
        }

        [HttpGet("calculator/result/{id}/csv")]
        public IActionResult DownloadCsv(int id)
        {
            var calc = _db.Hesaplamalar.Where(c => c.Id == id).Select(c => new
            {
                c.TotalAssets,
                c.Receivables,
                c.Debts,
                c.NetEstate,
                Shares = c.Shares.Select(s => new { s.HeirDisplay, s.FractionNumerator, s.FractionDenominator, s.Amount })
            }).FirstOrDefault();

            if (calc == null)
            {
                return NotFound();
            }

            // Türkçe karakterleri İngilizce karşılıklarına çeviren fonksiyon
            string RemoveTurkishChars(string text)
            {
                if (string.IsNullOrEmpty(text)) return "";
                return text
                    .Replace("ç", "c").Replace("Ç", "C")
                    .Replace("ğ", "g").Replace("Ğ", "G")
                    .Replace("ı", "i").Replace("İ", "I")
                    .Replace("ö", "o").Replace("Ö", "O")
                    .Replace("ş", "s").Replace("Ş", "S")
                    .Replace("ü", "u").Replace("Ü", "U");
            }

            // CSV içeriği oluştur
            var csv = new System.Text.StringBuilder();
            
            // Başlık
            csv.AppendLine(RemoveTurkishChars("Miras Paylasim Sonuclari"));
            var months = new[] { "Ocak", "Subat", "Mart", "Nisan", "Mayis", "Haziran", 
                                "Temmuz", "Agustos", "Eylul", "Ekim", "Kasim", "Aralik" };
            var monthName = RemoveTurkishChars(months[DateTime.Now.Month - 1]);
            csv.AppendLine($"Tarih: {DateTime.Now:dd} {monthName} {DateTime.Now:yyyy}");
            csv.AppendLine();
            
            // Özet bilgiler
            csv.AppendLine(RemoveTurkishChars("Ozet Bilgiler"));
            csv.AppendLine($"{RemoveTurkishChars("Toplam Malvarlik")};{calc.TotalAssets:N2} TL");
            csv.AppendLine($"{RemoveTurkishChars("Alacaklar")};{calc.Receivables:N2} TL");
            csv.AppendLine($"{RemoveTurkishChars("Borclar")};{calc.Debts:N2} TL");
            csv.AppendLine($"{RemoveTurkishChars("Net Tereke")};{calc.NetEstate:N2} TL");
            csv.AppendLine();
            
            // Tablo başlıkları
            csv.AppendLine($"{RemoveTurkishChars("Mirasci")};Kesir;Tutar (TL)");
            
            // Tablo verileri
            foreach (var share in calc.Shares)
            {
                var heirDisplay = RemoveTurkishChars(share.HeirDisplay).Replace(";", ",");
                var fraction = $"{share.FractionNumerator}/{share.FractionDenominator}";
                var amount = $"{share.Amount:N2} TL";
                csv.AppendLine($"{heirDisplay};{fraction};{amount}");
            }
            
            // Toplam
            csv.AppendLine($"{RemoveTurkishChars("TOPLAM")};;{calc.NetEstate:N2} TL");
            csv.AppendLine();
            
            // Yasal uyarı
            csv.AppendLine(RemoveTurkishChars("Not: Bu hesaplama Turk Medeni Kanunu'na gore yapilmistir."));
            csv.AppendLine(RemoveTurkishChars("Detayli hukuki danismanlik icin bir avukata basvurmaniz onerilir."));

            // CSV dosyasını döndür (UTF-8 BOM ile Excel uyumluluğu için)
            var csvContent = csv.ToString();
            var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(csvContent)).ToArray();
            return File(bytes, "text/csv; charset=utf-8", $"miras-paylasim-{id}.csv");
        }

        // Geri butonu action'ları
        [HttpGet("calculator/back")]
        public IActionResult Back(string currentStep)
        {
            var calc = GetCalcFromTemp();
            
            // Eğer calc yoksa Start'a dön
            if (calc == null && currentStep != "Start")
            {
                return RedirectToAction("Start");
            }

            // Her adım için bir önceki adımı belirle
            return currentStep switch
            {
                "Spouse" => RedirectToAction("Start"),
                "Children" => RedirectToAction("Spouse"),
                "Parents" => RedirectToAction("Children"),
                "SecondDegree" => RedirectToAction("Parents"),
                "ThirdDegree" => RedirectToAction("SecondDegree"),
                "Estate" => GetPreviousStepBeforeEstate(calc),
                "Debts" => RedirectToAction("Estate"),
                "Assistance" => RedirectToAction("Debts"),
                "AssistanceForm" => RedirectToAction("Assistance"),
                "UnfairlyTaken" => calc?.HasAssistance == true ? RedirectToAction("AssistanceForm") : RedirectToAction("Assistance"),
                "UnfairlyTakenForm" => RedirectToAction("UnfairlyTaken"),
                _ => RedirectToAction("Start")
            };
        }

        private IActionResult GetPreviousStepBeforeEstate(Calculation? calc)
        {
            if (calc == null)
            {
                return RedirectToAction("Start");
            }

            // Çocuk varsa Children'a dön
            if (calc.LivingChildrenCount > 0 || calc.HasGrandchildrenFromPredeceasedChild)
            {
                return RedirectToAction("Children");
            }

            // 3. zümre bilgileri varsa ThirdDegree'ye dön
            if (calc.PaternalGrandfatherAlive || calc.PaternalGrandmotherAlive ||
                calc.MaternalGrandfatherAlive || calc.MaternalGrandmotherAlive ||
                !string.IsNullOrEmpty(calc.PaternalGrandfatherUnclesAuntsAliveCount) ||
                !string.IsNullOrEmpty(calc.MaternalGrandfatherUnclesAuntsAliveCount))
            {
                return RedirectToAction("ThirdDegree");
            }

            // 2. zümre bilgileri varsa SecondDegree'ye dön
            if (calc.PaternalSiblingsAliveCount > 0 || calc.MaternalSiblingsAliveCount > 0 ||
                !string.IsNullOrEmpty(calc.PaternalPredeceasedSiblingsChildrenCounts) ||
                !string.IsNullOrEmpty(calc.MaternalPredeceasedSiblingsChildrenCounts))
            {
                return RedirectToAction("SecondDegree");
            }

            // Anne/baba bilgisi varsa Parents'a dön
            if (calc.FatherAlive || calc.MotherAlive)
            {
                return RedirectToAction("Parents");
            }

            // Hiçbiri yoksa Children'a dön (default)
            return RedirectToAction("Children");
        }

        private bool HasPredeceasedSiblingsChildren(string jsonCounts)
        {
            if (string.IsNullOrWhiteSpace(jsonCounts)) return false;
            try
            {
                var counts = System.Text.Json.JsonSerializer.Deserialize<List<int>>(jsonCounts?.Trim('[', ']', ' ') ?? "[]");
                return counts?.Any(c => c > 0) == true;
            }
            catch
            {
                return false;
            }
        }

        [HttpGet("calculator/history")]
        [Authorize]
        public IActionResult History(int page = 1, int pageSize = 10)
        {
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("History") });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("History") });
            }
            var totalCount = _db.Hesaplamalar.Count(c => c.UserId == userId);
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var calculations = _db.Hesaplamalar
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new
                {
                    c.Id,
                    c.CreatedAt,
                    c.TotalAssets,
                    c.Receivables,
                    c.Debts,
                    c.NetEstate,
                    HeirCount = c.Shares.Count
                })
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalCount = totalCount;
            ViewBag.HasPreviousPage = page > 1;
            ViewBag.HasNextPage = page < totalPages;

            return View(calculations);
        }
    }
}


