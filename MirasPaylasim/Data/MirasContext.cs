using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using MirasPaylasim.Models;

namespace MirasPaylasim.Data
{
    public class MirasContext : IdentityDbContext<ApplicationUser>
    {
        public MirasContext(DbContextOptions<MirasContext> options) : base(options)
        {
        }

        public DbSet<Calculation> Hesaplamalar => Set<Calculation>();
        public DbSet<Share> Paylar => Set<Share>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Calculation>().ToTable("Hesaplamalar")
                .Property(p => p.TotalAssets).HasPrecision(18, 2);
            modelBuilder.Entity<Calculation>()
                .Property(p => p.Receivables).HasPrecision(18, 2);
            modelBuilder.Entity<Calculation>()
                .Property(p => p.Debts).HasPrecision(18, 2);

            modelBuilder.Entity<Share>().ToTable("Paylar")
                .Property(p => p.Amount).HasPrecision(18, 2);
            modelBuilder.Entity<Share>()
                .Property(p => p.TheoreticalAmount).HasPrecision(18, 2);
            modelBuilder.Entity<Share>()
                .Property(p => p.ReservedPortion).HasPrecision(18, 2);
            modelBuilder.Entity<Share>()
                .Property(p => p.ViolationAmount).HasPrecision(18, 2);

            modelBuilder.Entity<Calculation>()
                .HasMany(c => c.Shares)
                .WithOne(s => s.Calculation)
                .HasForeignKey(s => s.CalculationId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // ApplicationUser ile Calculation ilişkisi
            modelBuilder.Entity<Calculation>()
                .HasOne(c => c.User)
                .WithMany(u => u.Calculations)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}


