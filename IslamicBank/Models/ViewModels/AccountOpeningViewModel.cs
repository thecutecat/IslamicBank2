using IslamicBank.Models.DomainModels;

namespace IslamicBank.Models.ViewModels
{
    public class AccountOpeningViewModel
    {
        public Guid CustomerId { get; set; }
        public AccountTypeViewModel AccountType { get; set; }
        public decimal InitialDeposit { get; set; }
        public decimal CustomerProfitRatio { get; set; } // For Mudarabah
        public decimal BankProfitRatio { get; set; }
    }

    public class AccountSummaryViewModel
    {
        public Guid AccountId { get; set; }
        public Guid CustomerId { get; set; }
        public AccountType AccountType { get; set; }
        public decimal Balance { get; set; }
        public string AccountNumber { get; set; }
    }

    public enum AccountTypeViewModel
    {
        WadiahCurrent,
        MudarabahSavings
    }

    public class DepositViewModel
    {
        public Guid AccountId { get; set; }
        public decimal Amount { get; set; }
        public string IdempotencyKey { get; set; } = Guid.NewGuid().ToString();
    }

    public class WithdrawalViewModel
    {
        public Guid AccountId { get; set; }
        public decimal Amount { get; set; }
        public string IdempotencyKey { get; set; } = Guid.NewGuid().ToString();
    }

    public class TransferViewModel
    {
        public Guid FromAccountId { get; set; }
        public Guid ToAccountId { get; set; }
        public decimal Amount { get; set; }
        public string IdempotencyKey { get; set; } = Guid.NewGuid().ToString();
    }

    public class TransferDailyTotalViewModel
    { 
        public Guid FromAccountId { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime Date { get; set; }
    }

    public class MurabahaViewModel
    {
        public Guid CustomerId { get; set; }
        public string AssetDescription { get; set; }
        public decimal CostPrice { get; set; }
        public decimal ProfitMarginPercentage { get; set; }
        public int InstallmentMonths { get; set; }
    }

    public class AccountBalanceViewModel
    {
        public Guid AccountId { get; set; }
        public decimal Balance { get; set; }
        public AccountType AccountType { get; set; }
        public decimal AvailableForWithdrawal { get; set; }
        public string LastTransactionReference { get; set; }
    }
}
