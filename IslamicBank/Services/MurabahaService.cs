using IslamicBank.Data;
using IslamicBank.Models.DomainModels;
using IslamicBank.Models.ViewModels;

namespace IslamicBank.Services
{
    public class MurabahaService:IMurabahaService
    {
        private readonly IslamicBankDbContext _context;
        private readonly ILogger<MurabahaService> _logger;

        public MurabahaService(IslamicBankDbContext context, ILogger<MurabahaService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<MurabahaContract> CreateContractAsync(MurabahaViewModel model)
        {
            // Enforce Islamic rule: Asset must be tangible
            if (string.IsNullOrWhiteSpace(model.AssetDescription))
                throw new ShariahComplianceException("Murabaha requires a tangible asset");

            var contract = MurabahaContract.Create(
                model.CustomerId,
                model.AssetDescription,
                model.CostPrice,
                model.ProfitMarginPercentage,
                model.InstallmentMonths);

            await _context.MurabahaContracts.AddAsync(contract);
            await _context.SaveChangesAsync();

            return contract;
        }

        public async Task<MurabahaContract> ExecuteContractAsync(Guid contractId)
        {
            var contract = await _context.MurabahaContracts.FindAsync(contractId);
            if (contract == null) throw new ArgumentException("Contract not found");

            contract.Execute();
            await _context.SaveChangesAsync();

            _logger.LogInformation("Murabaha contract {ContractId} executed for customer {CustomerId}",
                                   contract.Id, contract.CustomerId);

            return contract;
        }

        public async Task PayInstallmentAsync(Guid contractId, int installmentNumber)
        {
            var contract = await _context.MurabahaContracts.FindAsync(contractId);
            if (contract == null) throw new ArgumentException("Contract not found");

            contract.PayInstallment(installmentNumber);
            await _context.SaveChangesAsync();

            // No interest charged on late payment - Islamic rule enforced in domain model
        }

        public async Task<MurabahaContract> GetContractAsync(Guid contractId)
        {
            return await _context.MurabahaContracts.FindAsync(contractId);
        }
    }

    public interface IMurabahaService
    {
        Task<MurabahaContract> CreateContractAsync(MurabahaViewModel model);
        Task<MurabahaContract> ExecuteContractAsync(Guid contractId);
        Task PayInstallmentAsync(Guid contractId, int installmentNumber);
        Task<MurabahaContract> GetContractAsync(Guid contractId);
    }
}
