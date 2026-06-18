using IslamicBank.Data;
using IslamicBank.Infrastructure;
using IslamicBank.Models.DomainModels;
using IslamicBank.Models.ViewModels;
using IslamicBank.Repositories;
using Microsoft.EntityFrameworkCore;

namespace IslamicBank.Services
{
    public class AccountService : IAccountService
    {
            private readonly IslamicBankDbContext _context;
            private readonly IAccountRepository _accountRepository;
            private readonly IDistributedLockService _lockService;
            private readonly IIdempotencyService _idempotencyService;
            private readonly ILogger<AccountService> _logger;

            public AccountService(
                IslamicBankDbContext context,
                IAccountRepository accountRepository,
                IDistributedLockService lockService,
                IIdempotencyService idempotencyService,
                ILogger<AccountService> logger)
            {
                _context = context;
                _accountRepository = accountRepository;
                _lockService = lockService;
                _idempotencyService = idempotencyService;
                _logger = logger;
            }

        // Step 1: Account Opening
         
        public async Task<IslamicAccount> OpenAccountAsync(AccountOpeningViewModel model)
        {
            // Validate input
            if (model.CustomerId == Guid.Empty)
                 model.CustomerId = Guid.NewGuid(); 

            if (model.InitialDeposit < 0)
                throw new ArgumentException("Initial deposit cannot be negative");

            IslamicAccount account = null;

            // Enforce Islamic compliance based on account type
            if (model.AccountType == AccountTypeViewModel.MudarabahSavings)
            {
                // Validate profit sharing ratios
                if (model.CustomerProfitRatio <= 0 || model.BankProfitRatio <= 0)
                    throw new ShariahComplianceException("Profit sharing ratios must be positive");

                if (model.CustomerProfitRatio + model.BankProfitRatio != 1.0m)
                    throw new ShariahComplianceException("Profit sharing ratios must sum to 1.0 (100%)");

                // Create Mudarabah account
                account = IslamicAccount.OpenMudarabah(
                    model.CustomerId,
                    model.InitialDeposit,
                    model.CustomerProfitRatio,
                    model.BankProfitRatio);
            }
            else // WadiahCurrent
            {
                // Create Wadiah account (safekeeping, no profit)
                account = IslamicAccount.OpenWadiah(model.CustomerId, model.InitialDeposit);
            }

            // Save to database with transaction
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Add account to database
                await _context.IslamicAccounts.AddAsync(account);

                // Save changes
                await _context.SaveChangesAsync();

                // Commit transaction
                await transaction.CommitAsync();

                _logger.LogInformation("Account opened successfully. Account ID: {AccountId}, Type: {AccountType}, Customer: {CustomerId}",
                    account.Id, model.AccountType, model.CustomerId);

                return account;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Failed to open account for customer {CustomerId}", model.CustomerId);
                throw new Exception("Failed to open account. Please try again.", ex);
            }
        }

        // Step 2: Deposit with Idempotency
        public async Task<DepositResult> DepositAsync(DepositViewModel model)
            {
                // Check idempotency
                if (await _idempotencyService.RequestExistsAsync(model.IdempotencyKey))
                {
                    var cachedResult = await _idempotencyService.GetCachedResponseAsync<DepositResult>(model.IdempotencyKey);
                    _logger.LogInformation("Returning cached result for idempotency key {Key}", model.IdempotencyKey);
                    return cachedResult;
                }

                await using var dbTransaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // Use pessimistic lock for deposit
                    var account = await _accountRepository.GetByIdWithLockAsync(model.AccountId);
                    if (account == null)
                        return new DepositResult { Success = false, ErrorMessage = "Account not found" };

                    account.Deposit(model.Amount);
                    await _accountRepository.UpdateAsync(account);

                    var result = new DepositResult
                    {
                        Success = true,
                        NewBalance = account.Balance,
                        TransactionReference = Guid.NewGuid().ToString()
                    };

                    // Cache response for idempotency
                    await _idempotencyService.CacheResponseAsync(model.IdempotencyKey, result);

                    await _context.SaveChangesAsync();
                    await dbTransaction.CommitAsync();

                    return result;
                }
                catch (Exception ex)
                {
                    await dbTransaction.RollbackAsync();
                    _logger.LogError(ex, "Deposit failed for account {AccountId}", model.AccountId);
                    return new DepositResult { Success = false, ErrorMessage = ex.Message };
                }
            }

            // Step 3: Withdrawal with Optimistic Concurrency
            public async Task<WithdrawalResult> WithdrawAsync(WithdrawalViewModel model)
            {
                if (await _idempotencyService.RequestExistsAsync(model.IdempotencyKey))
                {
                    return await _idempotencyService.GetCachedResponseAsync<WithdrawalResult>(model.IdempotencyKey);
                }

                const int maxRetries = 3;
                for (int retry = 0; retry < maxRetries; retry++)
                {
                    try
                    {
                        await using var dbTransaction = await _context.Database.BeginTransactionAsync();

                        var account = await _accountRepository.GetByIdAsync(model.AccountId);
                        if (account == null)
                            return new WithdrawalResult { Success = false, ErrorMessage = "Account not found" };

                        account.Withdraw(model.Amount);
                        await _accountRepository.UpdateAsync(account);

                        var result = new WithdrawalResult
                        {
                            Success = true,
                            NewBalance = account.Balance,
                            TransactionReference = Guid.NewGuid().ToString()
                        };

                        await _idempotencyService.CacheResponseAsync(model.IdempotencyKey, result);
                        await _context.SaveChangesAsync();
                        await dbTransaction.CommitAsync();

                        return result;
                    }
                    catch (DbUpdateConcurrencyException ex) when (retry < maxRetries - 1)
                    {
                        _logger.LogWarning(ex, "Concurrency conflict on withdrawal, retrying {Retry}", retry + 1);
                        await Task.Delay(100 * (retry + 1));

                        // Reload entity
                        foreach (var entry in ex.Entries)
                            await entry.ReloadAsync();
                    }
                    catch (InsufficientFundsException ex)
                    {
                        return new WithdrawalResult { Success = false, ErrorMessage = ex.Message };
                    }
                }

                return new WithdrawalResult { Success = false, ErrorMessage = "Concurrency conflict, please retry" };
            }

            // Step 4: Fund Transfer with Redis Distributed Lock
            public async Task<TransferResult> TransferAsync(TransferViewModel model)
            {
                if (await _idempotencyService.RequestExistsAsync(model.IdempotencyKey))
                {
                    return await _idempotencyService.GetCachedResponseAsync<TransferResult>(model.IdempotencyKey);
                }

                // Use distributed lock to prevent double spending
                var lockKey = $"transfer:{model.FromAccountId}:{model.ToAccountId}";
                using var lockHandle = await _lockService.AcquireLockAsync(lockKey, TimeSpan.FromSeconds(10));

                await using var dbTransaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

                try
                {
                    // Get both accounts with locks
                    var fromAccount = await _accountRepository.GetByIdWithLockAsync(model.FromAccountId);
                    var toAccount = await _accountRepository.GetByIdWithLockAsync(model.ToAccountId);

                    if (fromAccount == null || toAccount == null)
                        return new TransferResult { Success = false, ErrorMessage = "Account not found" };

                    // Create transfer domain entity
                    var transfer = Transfer.Create(model.FromAccountId, model.ToAccountId, model.Amount);

                    // Execute transfer
                    fromAccount.Withdraw(model.Amount);
                    toAccount.Deposit(model.Amount);
                    transfer.Settle();

                    await _accountRepository.UpdateAsync(fromAccount);
                    await _accountRepository.UpdateAsync(toAccount);
                    await _context.Transfers.AddAsync(transfer);

                    var result = new TransferResult
                    {
                        Success = true,
                        TransferId = transfer.Id
                    };

                    await _idempotencyService.CacheResponseAsync(model.IdempotencyKey, result);
                    await _context.SaveChangesAsync();
                    await dbTransaction.CommitAsync();

                    return result;
                }
                catch (InsufficientFundsException ex)
                {
                    await dbTransaction.RollbackAsync();
                    return new TransferResult { Success = false, ErrorMessage = ex.Message };
                }
                catch (Exception ex)
                {
                    await dbTransaction.RollbackAsync();
                    _logger.LogError(ex, "Transfer failed");
                    return new TransferResult { Success = false, ErrorMessage = "Transfer failed" };
                }
            }

            public async Task<AccountBalanceViewModel> GetBalanceAsync(Guid accountId)
            {
                var account = await _accountRepository.GetByIdAsync(accountId);
                if (account == null) return null;

                return new AccountBalanceViewModel
                {
                    AccountId = account.Id,
                    Balance = account.Balance,
                    AccountType = account.Type,
                    AvailableForWithdrawal = account.Type == AccountType.WadiahCurrent ?
                                             account.Balance : account.Balance * 0.9m
                    // Mudarabah may have withdrawal restrictions
                };
            }

        public async Task<TransferDailyTotalViewModel> GetTransferDailyTotalAsync(Guid accountId)
        {
            var account = await _accountRepository.GetTransferDailyTotalAsync(accountId, DateTime.UtcNow);
            if (account == null) return null;

            return new TransferDailyTotalViewModel
            {
                FromAccountId = account.FromAccountId,
                TotalAmount = account.TotalAmount,
                Date = account.Date
            };
        }

        public Task<TransferDailyTotalViewModel> GetTransferDailyTotalAsync(Guid id, DateTime _dateTime)
        {
            var totalTransfer = _accountRepository.GetTransferDailyTotalAsync(id, _dateTime);
            if (totalTransfer == null) return Task.FromResult(new TransferDailyTotalViewModel() { Date = _dateTime.Date, FromAccountId = id, TotalAmount = 0 });
            
            return totalTransfer;
        }

        public Task<TransferDailyTotalViewModel> GetRecentTransferTransactionAsync(Guid id, DateTime _dateTime)
        {
            var totalTransfer = _accountRepository.GetTransferDailyTotalAsync(id, _dateTime);
            if (totalTransfer == null) return Task.FromResult(new TransferDailyTotalViewModel() { Date = _dateTime.Date, FromAccountId = id, TotalAmount = 0 });
            
            return totalTransfer;
        }

        public async Task<dynamic> GetListAccountSummaryAsync()
        {
            var data = await _accountRepository.GetListAccountSummaryAsync();
            return data;
        }

        public async Task<TransferViewModel> GetRecentTransferTransactionAsync(Guid id)
        {
            var data = await _accountRepository.GetRecentTransferTransactionAsync(id);
            return new TransferViewModel() { FromAccountId = data.FromAccountId, ToAccountId = data.ToAccountId, Amount = data.Amount };
        }
    }
}

