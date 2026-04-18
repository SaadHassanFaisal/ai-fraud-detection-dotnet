using FinancialApp.DAL.EF.Context;
using FinancialApp.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace FinancialApp.BLL.Services
{
    /// <summary>
    /// BLL Authentication Service. Validates credentials via BCrypt, logs every login
    /// attempt (success and failure) to AuditLog, and manages role-based access.
    /// Uses EF Core for user lookup — simple read operation, no performance concern.
    /// </summary>
    public class AuthenticationService
    {
        private readonly FinancialDbContext _context;
        private readonly AuditService _auditService;

        public AuthenticationService(FinancialDbContext context, AuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        /// <summary>
        /// Authenticates a user by verifying their BCrypt-hashed password.
        /// Every attempt (success or failure) is logged to the AuditLog table.
        /// </summary>
        public async Task<User?> AuthenticateAsync(string username, string plainTextPassword)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(plainTextPassword))
                return null;

            // EF Core used here — simple lookup by username, no complex query needed
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
            {
                await _auditService.LogLoginAttemptAsync(username, false);
                return null;
            }

            // BCrypt.Net verification of hashed password — never compare plain text
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(plainTextPassword, user.PasswordHash);

            // Audit log every login attempt — standard in financial systems
            await _auditService.LogLoginAttemptAsync(username, isPasswordValid,
                isPasswordValid ? user.UserId : null);

            return isPasswordValid ? user : null;
        }

        /// <summary>
        /// Hashes a plain text password using BCrypt with automatic salt generation.
        /// </summary>
        public string HashPassword(string plainTextPassword)
        {
            return BCrypt.Net.BCrypt.HashPassword(plainTextPassword);
        }

        /// <summary>
        /// Ensures a default admin user exists in the database for first-run and demo scenarios.
        /// Overwrites the password hash if the admin already exists (handles seed data migration).
        /// </summary>
        public async Task EnsureAdminExistsAsync()
        {
            var existingAdmin = await _context.Users.FirstOrDefaultAsync(u => u.Username == "admin");

            if (existingAdmin != null)
            {
                // Overwrite the seeded dummy hash with a real BCrypt hash
                existingAdmin.PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123");
                existingAdmin.Role = "Admin";
                _context.Users.Update(existingAdmin);
            }
            else
            {
                var newAdmin = new User
                {
                    Username = "admin",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                    Role = "Admin"
                };
                _context.Users.Add(newAdmin);
            }

            await _context.SaveChangesAsync();
        }
    }
}
