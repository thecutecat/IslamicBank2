using IslamicBank.Data;
using IslamicBank.Library;
using IslamicBank.Models;
using IslamicBank.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Text.Json;

namespace IslamicBank.Services
{
    public class AuditService : IAuditService
    {
        private readonly IslamicBankDbContext _context;
        private readonly ICurrentUserService _userService;
        private readonly IActionContextProvider _contextProvider;
        private readonly ILogger<AuditService> _logger;
        private readonly AuditLogFilter _auditFilter;


        public AuditService(IslamicBankDbContext context, ICurrentUserService userService, IActionContextProvider contextProvider, ILogger<AuditService> logger, AuditLogFilter auditFilter)
        {
            _context = context;
            _userService = userService;
            _contextProvider = contextProvider;                 
            _logger = logger;
            _auditFilter = auditFilter;
        }

         
        public async Task LogTransferStart(TransferViewModel request)
        {
            string previousHash = await GetPreviousHashAsync();
            var attribute = GetAuditAttribute();
            string recordContent = _auditFilter.ComputeSha256(attribute?.ActionType + DateTime.UtcNow);

            var audit = new AuditLog
            {
                Id = Guid.NewGuid(),
                ActionType = "TRANSFER_START",
                Timestamp = DateTime.UtcNow,
                UserId = _userService.GetCurrentUserId(),
                RequestData = JsonSerializer.Serialize(request),
                PreviousRecordHash = previousHash,
                CurrentRecordHash = recordContent,
                Success = true, // Unknown yet
                CorrelationId = Activity.Current?.Id ?? Guid.NewGuid().ToString(),
                CreatedAt = DateTime.UtcNow
            };

            await _context.AuditLogs.AddAsync(audit);
            await _context.SaveChangesAsync();
        }

        public async Task LogTransferSuccess(TransferViewModel request, string transactionId)
        {
            string previousHash = await GetPreviousHashAsync();
            var attribute = GetAuditAttribute();
            string recordContent = _auditFilter.ComputeSha256(attribute?.ActionType + DateTime.UtcNow);

            var audit = new AuditLog
            {
                Id = Guid.NewGuid(),
                ActionType = "TRANSFER_SUCCESS",
                Timestamp = DateTime.UtcNow,
                UserId = _userService.GetCurrentUserId(),
                RequestData = JsonSerializer.Serialize(request),
                ResponseStatus = "200",
                PreviousRecordHash = previousHash,
                CurrentRecordHash = recordContent,
                Success = true,
                ContractReference = transactionId, // Islamic banking reference
                CorrelationId = Activity.Current?.Id ?? Guid.NewGuid().ToString(),
                CreatedAt = DateTime.UtcNow
            };

            await _context.AuditLogs.AddAsync(audit);
            await _context.SaveChangesAsync();
        }

        public async Task LogTransferFailure(TransferViewModel request, string errorMessage)
        {
            string previousHash = await GetPreviousHashAsync();
            var attribute = GetAuditAttribute(); 
            string recordContent = _auditFilter.ComputeSha256(attribute?.ActionType + DateTime.UtcNow);

            var audit = new AuditLog
            {
                Id = Guid.NewGuid(),
                ActionType = "TRANSFER_FAILURE",
                Timestamp = DateTime.UtcNow,
                UserId = _userService.GetCurrentUserId(),
                RequestData = JsonSerializer.Serialize(request),
                PreviousRecordHash = previousHash,
                CurrentRecordHash = recordContent,
                Success = false,
                ErrorMessage = errorMessage,
                CorrelationId = Activity.Current?.Id ?? Guid.NewGuid().ToString(),
                CreatedAt = DateTime.UtcNow
            };

            await _context.AuditLogs.AddAsync(audit);
            await _context.SaveChangesAsync();
        }

        private async Task<string> GetPreviousHashAsync()
        {
            var lastRecord = await _context.AuditLogs
                .OrderByDescending(x => x.Timestamp)
                .FirstOrDefaultAsync();
            return lastRecord?.CurrentRecordHash ?? "0010-Init-Value"; 
        }

        public AuditLogAttribute? GetAuditAttribute()
        {
            var context = _contextProvider.GetActionExecutingContext();
            if (context == null)
            {
                _logger.LogWarning("No ActionExecutingContext available");
                return null;
            }

            // Check if the action method has the attribute
            var methodAttribute = context.ActionDescriptor.EndpointMetadata
                .OfType<AuditLogAttribute>()
                .FirstOrDefault();

            if (methodAttribute != null)
                return methodAttribute;

            // Check if the controller has the attribute (class-level)
            var controllerAttribute = context.Controller.GetType()
                .GetCustomAttributes(typeof(AuditLogAttribute), true)
                .FirstOrDefault() as AuditLogAttribute;

            return controllerAttribute;
        }
         
    }
}
