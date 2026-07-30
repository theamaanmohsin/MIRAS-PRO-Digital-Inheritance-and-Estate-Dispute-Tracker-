using InheritanceSystem.Data;
using InheritanceSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace inheritance_system.Services
{
    public class SecuritySettingsService
    {
        public const string AdminSecretKey = "AdminAccessSecret";

        private readonly AppDbContext _db;

        public SecuritySettingsService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<string> GetAdminSecretAsync()
        {
            var row = await _db.SystemSettings
                .FirstOrDefaultAsync(s => s.Key == AdminSecretKey);

            if (row != null)
                return row.Value;

            row = new SystemSetting
            {
                Key = AdminSecretKey,
                Value = AppConstants.DefaultAdminAccessSecret,
                UpdatedAt = DateTime.Now
            };
            _db.SystemSettings.Add(row);
            await _db.SaveChangesAsync();
            return row.Value;
        }

        public async Task<bool> ValidateAdminSecretAsync(string? input) =>
            string.Equals(input?.Trim(), await GetAdminSecretAsync(), StringComparison.Ordinal);

        public async Task<(bool Ok, string Message)> UpdateAdminSecretAsync(
            int adminId, string currentSecret, string newSecret, string confirmNewSecret)
        {
            if (string.IsNullOrWhiteSpace(newSecret) || newSecret.Length < 8)
                return (false, "New secret must be at least 8 characters.");

            if (newSecret != confirmNewSecret)
                return (false, "New secret and confirmation do not match.");

            if (!await ValidateAdminSecretAsync(currentSecret))
                return (false, "Current security key is incorrect.");

            var row = await _db.SystemSettings
                .FirstOrDefaultAsync(s => s.Key == AdminSecretKey);

            if (row == null)
            {
                row = new SystemSetting { Key = AdminSecretKey, Value = newSecret.Trim() };
                _db.SystemSettings.Add(row);
            }
            else
            {
                row.Value = newSecret.Trim();
            }

            row.UpdatedAt = DateTime.Now;
            row.UpdatedByAdminId = adminId;
            await _db.SaveChangesAsync();

            return (true, "Administrator security key updated successfully.");
        }
    }
}
