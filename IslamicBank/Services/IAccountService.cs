using IslamicBank.Models.DomainModels;
using IslamicBank.Models.ViewModels;

namespace IslamicBank.Services
{
    public interface IAccountService
    {
        Task<IslamicAccount> OpenAccountAsync(AccountOpeningViewModel model);
        Task<DepositResult> DepositAsync(DepositViewModel model);
        Task<WithdrawalResult> WithdrawAsync(WithdrawalViewModel model);
        Task<TransferResult> TransferAsync(TransferViewModel model);
        Task<TransferDailyTotalViewModel> GetTransferDailyTotalAsync(Guid id, DateTime _dateTime);
        Task<AccountBalanceViewModel> GetBalanceAsync(Guid accountId);
        Task<dynamic> GetListAccountSummaryAsync();
        Task<TransferViewModel> GetRecentTransferTransactionAsync(Guid id);
    }

    public class DepositResult
    {
        public bool Success { get; set; }
        public decimal NewBalance { get; set; }
        public string TransactionReference { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class WithdrawalResult
    {
        public bool Success { get; set; }
        public decimal NewBalance { get; set; }
        public string TransactionReference { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class TransferResult
    {
        public bool Success { get; set; }
        public Guid TransferId { get; set; }
        public string ErrorMessage { get; set; }
    }
}
