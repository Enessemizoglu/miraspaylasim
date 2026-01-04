namespace MirasPaylasim.Models
{
    public enum HeirType
    {
        Spouse,
        Child,
        Grandchild,
        Parent,
        Other
    }

    public class Share
    {
        public int Id { get; set; }
        public int CalculationId { get; set; }
        public Calculation Calculation { get; set; } = null!;

        public HeirType HeirType { get; set; }
        public string HeirDisplay { get; set; } = string.Empty;

        public int FractionNumerator { get; set; }
        public int FractionDenominator { get; set; }
        public decimal Amount { get; set; }

        public decimal TheoreticalAmount { get; set; } // Denkleþtirme öncesi brüt yasal pay
        public decimal ReservedPortion { get; set; } // Saklý pay tutarý
        public bool IsReservedPortionViolated { get; set; } // Ýhlal var mý?
        public decimal ViolationAmount { get; set; } // Ýhlal miktar
    }
}


