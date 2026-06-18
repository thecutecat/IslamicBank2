using IslamicBank.Models;
using IslamicBank.Models.DomainModels;
using Microsoft.EntityFrameworkCore;

namespace IslamicBank.Data
{
    public class IslamicBankDbContext : DbContext
    {
        public DbSet<IslamicAccount> IslamicAccounts { get; set; }
        public DbSet<MurabahaContract> MurabahaContracts { get; set; }
        public DbSet<Transfer> Transfers { get; set; }
        public DbSet<IdempotencyRecord> IdempotencyRecords { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        

        public IslamicBankDbContext(DbContextOptions<IslamicBankDbContext> options)
            : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Account configuration
            modelBuilder.Entity<IslamicAccount>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Balance).HasPrecision(18, 2);
                entity.Property(e => e.ProfitSharingRatioCustomer).HasPrecision(5, 4);
                entity.Property(e => e.ProfitSharingRatioBank).HasPrecision(5, 4);
                entity.Property(e => e.RowVersion).IsRowVersion(); // Concurrency token
            });

            // Murabaha configuration
            modelBuilder.Entity<MurabahaContract>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.CostPrice).HasPrecision(18, 2);
                entity.Property(e => e.SellingPrice).HasPrecision(18, 2);
                entity.Property(e => e.ProfitMarginPercentage).HasPrecision(5, 2);
                entity.Property(e => e.RowVersion).IsRowVersion();

                entity.OwnsMany(e => e.Installments, install =>
                {
                    install.WithOwner().HasForeignKey("ContractId");
                    install.Property(i => i.Amount).HasPrecision(18, 2);
                    install.Property(i => i.LateFeeToCharity).HasPrecision(18, 2);
                });
            });

            // Transfer configuration
            modelBuilder.Entity<Transfer>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Amount).HasPrecision(18, 2);
            });

            // Idempotency configuration
            modelBuilder.Entity<IdempotencyRecord>(entity =>
            {
                entity.HasKey(e => e.Key);
                entity.Property(e => e.Key).HasMaxLength(200);
                entity.HasIndex(e => e.ExpiresAt);
            });
        }
    }

    public class IdempotencyRecord
    {
        public string Key { get; set; }
        public string Response { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
