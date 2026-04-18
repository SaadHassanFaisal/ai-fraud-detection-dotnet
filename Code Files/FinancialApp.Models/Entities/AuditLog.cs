using System;
using System.ComponentModel.DataAnnotations;

namespace FinancialApp.Models.Entities
{
    public class AuditLog
    {
        [Key]
        public int LogId { get; set; }

        public int? UserId { get; set; } // Who did it (nullable if system generated)

        [Required]
        [MaxLength(100)]
        public string Action { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [Required]
        public string Details { get; set; } = string.Empty; // Store extra info as JSON string if needed
    }
}