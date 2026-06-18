using IslamicBank.Library;

namespace IslamicBank.Models.DomainModels
{
    public class MurabahaContract
    {
        public Guid Id { get; private set; }
        public Guid CustomerId { get; private set; }
        public string AssetDescription { get; private set; }
        public decimal CostPrice { get; private set; }
        public decimal ProfitMarginPercentage { get; private set; }
        public decimal SellingPrice { get; private set; }
        public int InstallmentMonths { get; private set; }
        public DateTime StartDate { get; private set; }
        public ContractStatus Status { get; private set; }
        public List<Installment> Installments { get; private set; } = new();
        public byte[] RowVersion { get; private set; }

        private MurabahaContract() { }

        public static MurabahaContract Create(Guid customerId, string assetDescription,
                                               decimal costPrice, decimal profitMargin,
                                               int installmentMonths)
        {
            if (profitMargin <= 0) throw new ShariahComplianceException("Profit margin must be positive");
            if (installmentMonths <= 0) throw new ArgumentException("Invalid installment period");

            var contract = new MurabahaContract
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                AssetDescription = assetDescription,
                CostPrice = costPrice,
                ProfitMarginPercentage = profitMargin,
                SellingPrice = costPrice * (1 + profitMargin / 100),
                InstallmentMonths = installmentMonths,
                Status = ContractStatus.Draft,
                RowVersion = Array.Empty<byte>()
            };

            contract.GenerateInstallments();
            return contract;
        }

        private void GenerateInstallments()
        {
            var monthlyAmount = SellingPrice / InstallmentMonths;
            for (int i = 1; i <= InstallmentMonths; i++)
            {
                Installments.Add(new Installment
                {
                    InstallmentNumber = i,
                    DueDate = StartDate.AddMonths(i),
                    Amount = monthlyAmount,
                    IsPaid = false
                });
            }
        }

        public void Execute()
        {
            if (Status != ContractStatus.Draft)
                throw new InvalidOperationException("Contract already executed");

            // Islamic rule: Asset must exist (simplified check)
            if (string.IsNullOrWhiteSpace(AssetDescription))
                throw new ShariahComplianceException("Asset must be specified for Murabaha");

            Status = ContractStatus.Active;
            StartDate = DateTime.UtcNow;
            GenerateInstallments(); // Regenerate with actual start date
        }

        public void PayInstallment(int installmentNumber)
        {
            var installment = Installments.FirstOrDefault(i => i.InstallmentNumber == installmentNumber);
            if (installment == null) throw new ArgumentException("Invalid installment number");
            if (installment.IsPaid) throw new InvalidOperationException("Installment already paid");
            if (installment.DueDate > DateTime.UtcNow.AddDays(10))
                throw new InvalidOperationException("Installment not yet due");

            installment.IsPaid = true;
            installment.PaidDate = DateTime.UtcNow;

            // No interest on late payment - Islamic rule
            if (installment.DueDate < DateTime.UtcNow && !installment.LateFeeToCharityProcessed)
            {
                // Late fee goes to charity, not bank revenue
                installment.LateFeeToCharity = 50; // Fixed penalty to charity
                installment.LateFeeToCharityProcessed = true;
            }
        }
    }

    public class Installment
    {
        public int InstallmentNumber { get; set; }
        public DateTime DueDate { get; set; }
        public decimal Amount { get; set; }
        public bool IsPaid { get; set; }
        public DateTime? PaidDate { get; set; }
        public decimal LateFeeToCharity { get; set; }
        public bool LateFeeToCharityProcessed { get; set; }
    }

    public enum ContractStatus
    {
        Draft,
        Active,
        Completed,
        Defaulted
    }
}
