using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace FinancialApp.Models.Entities
{
    public class Transaction
    {
        [Key]
        public int TxId { get; set; }

        [Required]
        public int AccountId { get; set; } // Foreign Key

        public int? CategoryId { get; set; } // Foreign Key (Nullable for transfers)

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(20)]
        public string Type { get; set; } = string.Empty; // Deposit, Withdrawal, Transfer

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public bool IsFlag { get; set; } = false; // For ML Fraud detection

        // Navigation Properties
        public Account Account { get; set; } = null!;
        public Category? Category { get; set; }
    }
}