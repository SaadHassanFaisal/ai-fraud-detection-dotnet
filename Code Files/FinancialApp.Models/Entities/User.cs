using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Security.Principal;

namespace FinancialApp.Models.Entities
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Role { get; set; } = "Analyst"; // "Admin" or "Analyst"

        // Navigation Property: 1 User -> Many Accounts
        public ICollection<Account> Accounts { get; set; } = new List<Account>();
    }
}