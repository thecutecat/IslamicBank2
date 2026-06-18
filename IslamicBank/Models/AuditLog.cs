using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IslamicBank.Models
{
    [Table("AuditLogs", Schema = "audit")] // Separate schema for organization
    public class AuditLog
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string ActionType { get; set; } = string.Empty; // "TRANSFER", "WITHDRAWAL", "PROFIT_CALC"

        [Required]
        public DateTime Timestamp { get; set; }

        [Required]
        public int UserId { get; set; }

        [MaxLength(100)]
        public string? UserName { get; set; } // Denormalized for quick audit reports

        [Required]
        [MaxLength(100)]
        public string CorrelationId { get; set; } = string.Empty; // Trace end-to-end

        [Column(TypeName = "nvarchar(max)")]
        public string? RequestData { get; set; } // JSON of request

        [MaxLength(20)]
        public string? ResponseStatus { get; set; }

        public bool Success { get; set; }

        [MaxLength(500)]
        public string? ErrorMessage { get; set; }

        [MaxLength(45)]
        public string? IpAddress { get; set; }

        [MaxLength(10)]
        public string? HttpMethod { get; set; }

        [MaxLength(500)]
        public string? RequestPath { get; set; }

        // Tamper-evident fields
        [MaxLength(128)]
        public string? PreviousRecordHash { get; set; }

        [MaxLength(128)]
        public string? CurrentRecordHash { get; set; }

        // Islamic banking specific
        [MaxLength(50)]
        public string? ContractReference { get; set; } // For Murabaha, Ijara contracts

        [MaxLength(50)]
        public string? ShariahApprovalStatus { get; set; } // "PENDING", "APPROVED", "REJECTED"

        // Performance optimization 
       // [Index] // Add index for faster queries
        public DateTime CreatedAt { get; set; }
    }
}