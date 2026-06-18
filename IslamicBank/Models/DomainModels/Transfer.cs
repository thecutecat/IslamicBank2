namespace IslamicBank.Models.DomainModels
{
    public class Transfer
    {
        public Guid Id { get; private set; }
        public Guid FromAccountId { get; private set; }
        public Guid ToAccountId { get; private set; }
        public decimal Amount { get; private set; }
        public TransferStatus Status { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? SettledAt { get; private set; }

        private Transfer() { }

        public static Transfer Create(Guid fromAccountId, Guid toAccountId, decimal amount)
        {
            if (amount <= 0) throw new ArgumentException("Amount must be positive");

            return new Transfer
            {
                Id = Guid.NewGuid(),
                FromAccountId = fromAccountId,
                ToAccountId = toAccountId,
                Amount = amount,
                Status = TransferStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };
        }

        public void Settle()
        {
            if (Status != TransferStatus.Pending)
                throw new InvalidOperationException($"Cannot settle transfer with status {Status}");

            Status = TransferStatus.Settled;
            SettledAt = DateTime.UtcNow;
        }

        public void Fail(string reason)
        {
            if (Status != TransferStatus.Pending)
                throw new InvalidOperationException($"Cannot fail transfer with status {Status}");

            Status = TransferStatus.Failed;
        }
    }

    public enum TransferStatus
    {
        Pending,
        Settled,
        Failed
    }
}
