using Microsoft.AspNetCore.Identity;

namespace MirasPaylasim.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation property - Kullanıcının hesaplamaları
        public virtual ICollection<Calculation> Calculations { get; set; } = new List<Calculation>();
    }
}

