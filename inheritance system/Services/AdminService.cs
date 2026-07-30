using InheritanceSystem.Data;
using InheritanceSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace inheritance_system.Services
{
    public class AdminService
    {
        private readonly AppDbContext _db;

        public AdminService(AppDbContext db)
        {
            _db = db;
        }

        // ── Console stats ─────────────────────────────────────────
        public async Task<int> TotalUsersAsync() =>
            await _db.Users.CountAsync(u =>
                u.Role == AppConstants.RoleUser
                || u.Role == "Owner"
                || u.Role == "LegalProfessional");

        public async Task<int> TotalPropertiesAsync() =>
            await _db.Properties.CountAsync();

        public async Task<int> ActiveDisputesAsync() =>
            await _db.Disputes.CountAsync(d =>
                d.Status == "Pending" || d.Status == "Under Review");

        // ── Profile lock / unlock ─────────────────────────────────
        public async Task LockUserAsync(int userId)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user == null) return;
            user.IsEditable = false;
            await _db.SaveChangesAsync();
        }

        public async Task UnlockUserAsync(int userId)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user == null) return;
            user.IsEditable = true;
            await _db.SaveChangesAsync();
        }

        // ── Audit log ─────────────────────────────────────────────
        public async Task LogAsync(int adminId, string adminName,
            string action, string? targetType = null,
            int? targetId = null, string? details = null)
        {
            _db.AdminAuditLogs.Add(new AdminAuditLog
            {
                AdminUserId = adminId,
                AdminName = adminName,
                Action = action,
                TargetType = targetType,
                TargetId = targetId,
                Details = details,
                CreatedAt = DateTime.Now
            });
            await _db.SaveChangesAsync();
        }

        public async Task<List<AdminAuditLog>> RecentLogsAsync(int count = 10)
        {
            try
            {
                return await _db.AdminAuditLogs
                    .OrderByDescending(l => l.CreatedAt)
                    .Take(count)
                    .ToListAsync();
            }
            catch
            {
                return new List<AdminAuditLog>();
            }
        }
    }
}