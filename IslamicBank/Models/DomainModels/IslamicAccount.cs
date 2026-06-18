namespace IslamicBank.Models.DomainModels
{
    public class IslamicAccount
    {
        public Guid Id { get; private set; }
        public Guid CustomerId { get; private set; }
        public AccountType Type { get; private set; }
        public decimal Balance { get; private set; }
        public decimal ProfitSharingRatioCustomer { get; private set; } // for Mudarabah
        public decimal ProfitSharingRatioBank { get; private set; }
        public byte[] RowVersion { get; private set; } // Concurrency handling

        // For EF Core
        private IslamicAccount() { }

        public static IslamicAccount OpenMudarabah(Guid customerId, decimal initialDeposit,
                                                    decimal customerRatio, decimal bankRatio)
        {
            if (customerRatio + bankRatio != 1.0m)
                throw new ShariahComplianceException("Profit sharing ratios must sum to 1");

            if (customerRatio <= 0 || bankRatio <= 0)
                throw new ShariahComplianceException("Ratios must be positive");

            

            return new IslamicAccount
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                Type = AccountType.MudarabahSavings,
                Balance = initialDeposit,
                ProfitSharingRatioCustomer = customerRatio,
                ProfitSharingRatioBank = bankRatio,
                RowVersion = Array.Empty<byte>()
            };
        }

        public static IslamicAccount OpenWadiah(Guid customerId, decimal initialDeposit)
        {
            return new IslamicAccount
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                Type = AccountType.WadiahCurrent,
                Balance = initialDeposit,
                ProfitSharingRatioCustomer = 0, // No profit sharing
                ProfitSharingRatioBank = 0,
                RowVersion = Array.Empty<byte>()
            };

        }

        public void Deposit(decimal amount)
        {
            if (amount <= 0) throw new ArgumentException("Amount must be positive");
            Balance += amount;
        }

        public void Withdraw(decimal amount)
        {
            if (amount <= 0) throw new ArgumentException("Amount must be positive");
            if (Balance < amount) throw new InsufficientFundsException($"Insufficient balance. Available: {Balance}");
            Balance -= amount;
        }

        public void AddProfit(decimal profitAmount)
        {
            if (Type != AccountType.MudarabahSavings)
                throw new ShariahComplianceException("Only Mudarabah accounts can receive profit");

            Balance += profitAmount;
        }
    }

    public enum AccountType
    {
        WadiahCurrent,     // Safekeeping - no profit
        MudarabahSavings   // Profit sharing
    }
}
