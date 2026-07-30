using Microsoft.EntityFrameworkCore;
using InheritanceSystem.Models;

namespace InheritanceSystem.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // ── DbSets ────────────────────────────────────────────────
        public DbSet<User> Users { get; set; }
        public DbSet<Property> Properties { get; set; }
        public DbSet<Heir> Heirs { get; set; }
        public DbSet<Dispute> Disputes { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<Transfer> Transfers { get; set; }

        // Changed back to 'Cases' to match your PropertyService.cs requirements
        public DbSet<InheritanceCase> Cases { get; set; }

        // ── New DbSets ────────────────────────────────────────────
        public DbSet<FrcDocument> FrcDocuments { get; set; }
        public DbSet<AdminRegistrationInvite> AdminRegistrationInvites { get; set; }
        public DbSet<AdminAuditLog> AdminAuditLogs { get; set; }
        public DbSet<SystemSetting> SystemSettings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Prevent EF cascade conflict on FrcDocuments
            modelBuilder.Entity<FrcDocument>()
                .HasOne(f => f.ReviewedByAdmin)
                .WithMany()
                .HasForeignKey(f => f.ReviewedByAdminId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<FrcDocument>()
                .HasOne(f => f.User)
                .WithMany()
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}