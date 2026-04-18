using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinancialApp.Models.Entities
{
    public class Alert
    {
        [Key]
        public int AlertId { get; set; }

        [Required]
        [ForeignKey("Transaction")]
        public int TxId { get; set; } // Foreign Key

        [Column(TypeName = "decimal(5,4)")]
        public decimal Confidence { get; set; } // e.g., 0.9950 for 99.5% fraud confidence

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsRead { get; set; } = false;

        // Navigation Property
        public Transaction Transaction { get; set; } = null!;
    }
}