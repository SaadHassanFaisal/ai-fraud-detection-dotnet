using FinancialApp.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinancialApp.DAL.EF.Context
{
    public class FinancialDbContext : DbContext
    {
        public FinancialDbContext(DbContextOptions<FinancialDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Account> Accounts { get; set; } = null!;
        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<Transaction> Transactions { get; set; } = null!;
        public DbSet<Alert> Alerts { get; set; } = null!;
        public DbSet<AuditLog> AuditLogs { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. Fluent API: Decimal Precisions
            modelBuilder.Entity<Account>().Property(a => a.Balance).HasPrecision(18, 2);
            modelBuilder.Entity<Transaction>().Property(t => t.Amount).HasPrecision(18, 2);
            modelBuilder.Entity<Alert>().Property(a => a.Confidence).HasPrecision(5, 4);

            // 2. Fluent API: Relationships & Foreign Keys
            modelBuilder.Entity<User>()
                .HasMany(u => u.Accounts)
                .WithOne(a => a.User)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Account>()
                .HasMany(a => a.Transactions)
                .WithOne(t => t.Account)
                .HasForeignKey(t => t.AccountId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.Category)
                .WithMany(c => c.Transactions)
                .HasForeignKey(t => t.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            // 3. Fluent API: Database Indexes
            modelBuilder.Entity<Account>().HasIndex(a => a.UserId);
            modelBuilder.Entity<Transaction>().HasIndex(t => t.AccountId);
            modelBuilder.Entity<Transaction>().HasIndex(t => t.Timestamp);
            modelBuilder.Entity<Transaction>().HasIndex(t => t.IsFlag);

            // 4. Data Seeding
            modelBuilder.Entity<User>().HasData(
                new User { UserId = 1, Username = "admin", PasswordHash = "$2a$11$dummyhash12345678901234567890", Role = "Admin" }
            );

            modelBuilder.Entity<Category>().HasData(
                new Category { CategoryId = 1, Name = "Income" },
                new Category { CategoryId = 2, Name = "Food" },
                new Category { CategoryId = 3, Name = "Transfer" },
                new Category { CategoryId = 4, Name = "Utilities" },
                new Category { CategoryId = 5, Name = "Entertainment" }
            );
        }
    }
}