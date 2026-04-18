using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Transactions;

namespace FinancialApp.Models.Entities
{
    public class Category
    {
        [Key]
        public int CategoryId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        // Navigation Property
        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }
}