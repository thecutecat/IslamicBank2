using IslamicBank.Models.ViewModels;
using Microsoft.AspNetCore.Mvc.Filters;

namespace IslamicBank.Services
{
    public interface IAuditService
    {  
        Task LogTransferStart(TransferViewModel request);
        Task LogTransferSuccess(TransferViewModel request, string transactionId);
        Task LogTransferFailure(TransferViewModel request, string errorMessage);
    }
}
