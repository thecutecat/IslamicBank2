using IslamicBank.Data;
using IslamicBank.Models.DomainModels;
using IslamicBank.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace IslamicBank.Repositories
{
    public class AccountRepository: IAccountRepository
    {
        private readonly IslamicBankDbContext _context;

        public AccountRepository(IslamicBankDbContext context)
        {
            _context = context;
        }

        public async Task<IslamicAccount> GetByIdAsync(Guid id)
        {
            return await _context.IslamicAccounts.FindAsync(id);
        }

        // Pessimistic locking for high-value transactions
        public async Task<IslamicAccount> GetByIdWithLockAsync(Guid id)
        {
            return await _context.IslamicAccounts
                .FromSqlRaw("SELECT * FROM IslamicAccounts WITH (UPDLOCK, ROWLOCK) WHERE Id = {0}", id)
                .FirstOrDefaultAsync();
        }

        // Pessimistic locking for high-value transactions
        public async Task<TransferDailyTotalViewModel> GetTransferDailyTotalAsync(Guid id, DateTime _dateTime)
        {
            var data = await _context.Transfers
                .FromSqlRaw("select * FROM [IslamicBank].[dbo].[Transfers] WITH (UPDLOCK, ROWLOCK) WHERE FromAccountId = {0} AND CAST([CreatedAt] AS DATE) = {1} AND Status=1", id, _dateTime.Date)
                .ToListAsync();

            return new TransferDailyTotalViewModel
            {
                FromAccountId = id,
                TotalAmount = data.Sum(t => t.Amount)
            };
        }

        public async Task AddAsync(IslamicAccount account)
        {
            await _context.IslamicAccounts.AddAsync(account);
        }

        public Task UpdateAsync(IslamicAccount account)
        {
            _context.IslamicAccounts.Update(account);
            return Task.CompletedTask;
        }

        public async Task<List<AccountSummaryViewModel>> GetListAccountSummaryAsync()
        {
            var data = await _context.IslamicAccounts.ToListAsync();
            
            
            return data.Select(acc => new AccountSummaryViewModel
            {
                AccountId = acc.Id,
                CustomerId = acc.CustomerId,
                Balance = acc.Balance,
                AccountType = acc.Type == 0 ? AccountType.WadiahCurrent : AccountType.MudarabahSavings,
                AccountNumber = $"ACC-{acc.Id.ToString().Substring(0, 8).ToUpper()}" //temp :change to customer name later
            }).ToList();
  
        }

        async Task<Transfer> IAccountRepository.GetRecentTransferTransactionAsync(Guid id)
        {
           var data = await _context.Transfers.Where(t => (t.FromAccountId == id || t.ToAccountId == id) && t.Status == TransferStatus.Settled)
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync();

            if(data == null)
                return null;

            return data;
        }
    }

    public interface IAccountRepository
    {
        Task<IslamicAccount> GetByIdAsync(Guid id);
        Task<IslamicAccount> GetByIdWithLockAsync(Guid id);
        Task<TransferDailyTotalViewModel> GetTransferDailyTotalAsync(Guid id, DateTime _dateTime);
        Task<Transfer> GetRecentTransferTransactionAsync(Guid id);
        // Task<AccountSummaryViewModel> GetAccountSummaryAsync(Guid id);
        Task<List<AccountSummaryViewModel>> GetListAccountSummaryAsync();
        Task AddAsync(IslamicAccount account);
        Task UpdateAsync(IslamicAccount account);
    }
}
