using IslamicBank.Data;
using IslamicBank.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace IslamicBank.Library
{
    public class AuditLogFilter : IAsyncActionFilter
    {
        private readonly IslamicBankDbContext _auditDbContext; 

        public AuditLogFilter(IslamicBankDbContext auditDbContext)//, ICurrentUserService userService)
        {
            _auditDbContext = auditDbContext; 
        }

        public async Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next)
        {
            // Capture BEFORE state
            var attribute = GetAuditAttribute(context);
            if (attribute == null)
            {
                await next();
                return;
            }

            // Get last record's hash
            var lastRecord = await _auditDbContext.AuditLogs
                .OrderByDescending(x => x.Timestamp)
                .FirstOrDefaultAsync();
            //.FirstOrDefaultAsync();

            string previousHash = lastRecord?.CurrentRecordHash ?? "0010-Init-PreEntry";

            // Create hash of THIS record
            string recordContent = SerializeRequestData(context.ActionArguments, attribute.SensitiveData);
            string CurrentRecordHash = ComputeSha256(recordContent);
            
            var auditEntry = new AuditLog
            {
                Id = Guid.NewGuid(),
                ActionType = attribute.ActionType,
                Timestamp = DateTime.UtcNow,
                UserId = 10, //_userService.GetCurrentUserId(), blm ada login
                CorrelationId = context.HttpContext.TraceIdentifier,
                RequestData = recordContent,
                PreviousRecordHash = previousHash, 
                CurrentRecordHash = CurrentRecordHash, 
                IpAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString()
            };

            // Execute the actual method
            var resultContext = await next();

            // Capture AFTER state
            auditEntry.ResponseStatus = resultContext.Result is ObjectResult obj
                ? obj.StatusCode.ToString()
                : "200";

            auditEntry.Success = resultContext.Exception == null;

            if (resultContext.Exception != null)
            {
                auditEntry.ErrorMessage = resultContext.Exception.Message;
            }

            // Save to audit database
            await _auditDbContext.AuditLogs.AddAsync(auditEntry);
            await _auditDbContext.SaveChangesAsync();
        }

        public async Task SaveAuditLogAsync(AuditLog newEntry)
        {
            // Get last record's hash
            var lastRecord = await _auditDbContext.AuditLogs
                .OrderByDescending(x => x.Timestamp)
                .FirstOrDefaultAsync(); 

            string previousHash = lastRecord?.CurrentRecordHash ?? "0000-Initial-Entry";

            // Create hash of THIS record
            string recordContent = $"{newEntry.Id}|{newEntry.ActionType}|{newEntry.Timestamp}|{newEntry.UserId}|{newEntry.RequestData}";
            newEntry.CurrentRecordHash = ComputeSha256(recordContent);

            // Chain to previous record
            string chainedContent = $"{previousHash}|{recordContent}";
            newEntry.PreviousRecordHash = ComputeSha256(chainedContent);

            await _auditDbContext.AuditLogs.AddAsync(newEntry);
            await _auditDbContext.SaveChangesAsync();

            // Also write to append-only file (defense in depth)
            await File.AppendAllTextAsync(@"\\SECURE-SHARE\audit\audit.log",
                $"{DateTime.UtcNow:O}|{newEntry.Id}|{chainedContent}");
        }

        /// <summary>
        /// Extracts the AuditLogAttribute from the action method or controller
        /// </summary>
        public AuditLogAttribute? GetAuditAttribute(ActionExecutingContext context)
        {
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

        /// <summary>
        /// Serializes request data safely (with sensitive data masking)
        /// </summary>
        public string SerializeRequestData(IDictionary<string, object?> actionArguments, bool sensitiveData)
        {
            if (actionArguments == null || !actionArguments.Any())
                return "{}";

            try
            {
                var serialized = JsonSerializer.Serialize(actionArguments, new JsonSerializerOptions
                {
                    WriteIndented = false,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                // Mask sensitive fields if needed
                if (!sensitiveData)
                {
                    serialized = MaskSensitiveData(serialized);
                }

                // Truncate if too long (audit tables have limits)
                if (serialized.Length > 8000)
                {
                    serialized = serialized.Substring(0, 8000) + "... [TRUNCATED]";
                }

                return serialized;
            }
            catch (Exception ex)
            {
                return $"{{\"error\": \"Failed to serialize: {ex.Message}\"}}";
            }
        }

        /// <summary>
        /// Masks sensitive fields like passwords, credit card numbers, etc.
        /// </summary>
        private string MaskSensitiveData(string json)
        {
            var sensitiveFields = new[] { "password", "pin", "cvv", "cardnumber", "nationalid" };

            foreach (var field in sensitiveFields)
            {
                var pattern = $"\"{field}\":\"[^\"]*\"";
                var replacement = $"\"{field}\":\"***MASKED***\"";
                json = System.Text.RegularExpressions.Regex.Replace(json, pattern, replacement,
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            }

            return json;
        }

        /// <summary>
        /// Gets HTTP status code from action result
        /// </summary>
        private string GetStatusCode(IActionResult result)
        {
            switch (result)
            {
                case ObjectResult objectResult:
                    return objectResult.StatusCode?.ToString() ?? "200";
                case StatusCodeResult statusCodeResult:
                    return statusCodeResult.StatusCode.ToString();
                //case Ok:
                 //   return "200";
                //case BadRequestResult:
                //    return "400";
                //case UnauthorizedResult:
                //    return "401";
                //case NotFoundResult:
                //    return "404";
                default:
                    return "200";
            }
        }

        /// <summary>
        /// Adds tamper-evident hash chain to the audit entry
        /// </summary>
        private async Task AddToHashChain(AuditLog auditEntry)
        {
            // Get the last audit record's hash
            var lastRecord = await _auditDbContext.AuditLogs
                .OrderByDescending(x => x.Timestamp)
                .FirstOrDefaultAsync();

            string previousHash = lastRecord?.CurrentRecordHash ?? "00000000-0000-0000-0000-000000000000";

            // Create content to hash for THIS record
            var recordContent = $"{auditEntry.Id}|{auditEntry.ActionType}|{auditEntry.Timestamp:O}|{auditEntry.UserId}|{auditEntry.RequestData}|{auditEntry.Success}";
            auditEntry.CurrentRecordHash = ComputeSha256(recordContent);

            // Chain with previous record
            var chainedContent = $"{previousHash}|{recordContent}";
            auditEntry.PreviousRecordHash = ComputeSha256(chainedContent);
        }

        /// <summary>
        /// Computes SHA256 hash of a string
        /// </summary>
        public string ComputeSha256(string input)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToHexString(hash).ToLower();
        }
    }
}

