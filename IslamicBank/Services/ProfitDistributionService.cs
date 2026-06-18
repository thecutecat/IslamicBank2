using IslamicBank.Data;
using IslamicBank.Models.DomainModels;
using Microsoft.EntityFrameworkCore;

namespace IslamicBank.Services
{
    public class ProfitDistributionService:IProfitDistributionService
    {
        private readonly IslamicBankDbContext _context;
        private readonly ILogger<ProfitDistributionService> _logger;

        public ProfitDistributionService(IslamicBankDbContext context, ILogger<ProfitDistributionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task DistributeMonthlyProfitsAsync(DateTime periodEnd)
        {
            // Check if already distributed for this period
            var lastDistribution = await _context.Set<ProfitDistributionLog>()
                .OrderByDescending(l => l.DistributedAt)
                .FirstOrDefaultAsync();
                //.FirstOrDefaultAsync();

            if (lastDistribution?.PeriodEnd.Date == periodEnd.Date)
            {
                _logger.LogWarning("Profit already distributed for period ending {PeriodEnd}", periodEnd);
                return;
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Get all Mudarabah accounts
                var mudarabahAccounts = await _context.IslamicAccounts
                    .Where(a => a.Type == AccountType.MudarabahSavings)
                    .ToListAsync();

                // Calculate total bank profit (simplified - would come from Shariah-compliant investments)
                var totalBankProfit = await CalculateTotalProfitAsync(periodEnd);

                foreach (var account in mudarabahAccounts)
                {
                    // Distribute based on profit sharing ratio
                    var customerShare = totalBankProfit * account.ProfitSharingRatioCustomer;

                    // Pro-rata based on account balance (simplified)
                    var accountShare = customerShare / mudarabahAccounts.Count;

                    account.AddProfit(accountShare);
                    _logger.LogInformation("Distributed {Amount} profit to account {AccountId}", accountShare, account.Id);
                }

                // Log distribution
                _context.Set<ProfitDistributionLog>().Add(new ProfitDistributionLog
                {
                    PeriodEnd = periodEnd,
                    DistributedAt = DateTime.UtcNow,
                    TotalProfit = totalBankProfit
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Profit distribution failed");
                throw;
            }
        }

        private async Task<decimal> CalculateTotalProfitAsync(DateTime periodEnd)
        {
            // In real system, this would sum profits from Shariah-compliant financing activities
            // No interest-bearing activities included
            return await Task.FromResult(100000m); // Placeholder
        }
    }

    public class ProfitDistributionLog
    {
        public int Id { get; set; }
        public DateTime PeriodEnd { get; set; }
        public DateTime DistributedAt { get; set; }
        public decimal TotalProfit { get; set; }
    }

    public interface IProfitDistributionService
    {
        Task DistributeMonthlyProfitsAsync(DateTime periodEnd);
    }
}
