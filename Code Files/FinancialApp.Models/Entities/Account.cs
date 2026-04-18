using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Transactions;

namespace FinancialApp.Models.Entities
{
    public class Account
    {
        [Key]
        public int AccountId { get; set; }

        [Required]
        public int UserId { get; set; } // Foreign Key

        [Column(TypeName = "decimal(18,2)")]
        public decimal Balance { get; set; }

        [Required]
        [MaxLength(50)]
        public string AccountType { get; set; } = string.Empty; // e.g., Checking, Savings

        // Navigation Properties
        public User User { get; set; } = null!;
        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }
}