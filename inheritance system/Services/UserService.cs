using InheritanceSystem.Data;
using InheritanceSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace inheritance_system.Services
{
    public class UserService
    {
        private readonly AppDbContext _db;
        private readonly SecuritySettingsService _security;

        public UserService(AppDbContext db, SecuritySettingsService security)
        {
            _db = db;
            _security = security;
        }

        public static bool IsUserRole(string? role) =>
            role == AppConstants.RoleUser
            || AppConstants.LegacyUserRoles.Contains(role ?? "");

        public async Task<(bool Success, string Message)> RegisterAsync(
            string firstName, string lastName,
            string email, string phone,
            string password, string role,
            string? cnic = null)
        {
            if (role != AppConstants.RoleUser)
                return (false, "Invalid account type. Select User or Administrator.");

            if (string.IsNullOrWhiteSpace(cnic))
                return (false, "CNIC is required.");

            var normalizedEmail = email.Trim().ToLower();
            if (await _db.Users.AnyAsync(u => u.Email == normalizedEmail))
                return (false, "An account with this email already exists.");

            var user = new User
            {
                FullName = $"{firstName.Trim()} {lastName.Trim()}".Trim(),
                Email = normalizedEmail,
                Phone = phone?.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Role = AppConstants.RoleUser,
                Cnic = cnic.Trim(),
                IsEditable = true,
                CreatedAt = DateTime.Now
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            return (true, "Account created successfully!");
        }

        public async Task<(bool Success, string Message)> RegisterAdminAsync(
            string firstName, string lastName,
            string email, string phone,
            string password, string adminSecret)
        {
            if (!await _security.ValidateAdminSecretAsync(adminSecret))
                return (false, "Invalid administrator security key.");

            var normalizedEmail = email.Trim().ToLower();
            if (await _db.Users.AnyAsync(u => u.Email == normalizedEmail))
                return (false, "An account with this email already exists.");

            var user = new User
            {
                FullName = $"{firstName.Trim()} {lastName.Trim()}".Trim(),
                Email = normalizedEmail,
                Phone = phone?.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Role = AppConstants.RoleAdmin,
                IsEditable = false,
                CreatedAt = DateTime.Now
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            return (true, "Administrator account created successfully!");
        }

        public async Task<(bool Success, string Message, User? User)> LoginAdminAsync(
            string email, string password, string adminSecret)
        {
            if (!await _security.ValidateAdminSecretAsync(adminSecret))
                return (false, "Invalid administrator security key.", null);

            return await LoginAsync(email, password, AppConstants.RoleAdmin);
        }

        public async Task<(bool Success, string Message, User? User)> LoginAsync(
            string email, string password, string? expectedRole = null)
        {
            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.Email == email.Trim().ToLower());

            if (user == null)
                return (false, "No account found with this email address.", null);

            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                return (false, "Incorrect password. Please try again.", null);

            if (expectedRole == AppConstants.RoleAdmin)
            {
                if (user.Role != AppConstants.RoleAdmin)
                    return (false, "This account is not an administrator. Use user login instead.", null);
            }
            else if (expectedRole == AppConstants.RoleUser)
            {
                if (user.Role == AppConstants.RoleAdmin)
                    return (false, "Administrator accounts must use the admin login portal.", null);
                if (!IsUserRole(user.Role))
                    return (false, "Invalid user account type.", null);
            }

            return (true, "Login successful!", user);
        }

        public async Task<User?> GetUserByIdAsync(int userId) =>
            await _db.Users.FindAsync(userId);

        public async Task<List<User>> GetAllUsersAsync() =>
            await _db.Users.OrderByDescending(u => u.CreatedAt).ToListAsync();

        public async Task<(bool Success, string Message)> DeleteUserAsync(int userId, int requestingAdminId)
        {
            if (userId == requestingAdminId)
                return (false, "You cannot delete your own account.");
            var user = await _db.Users.FindAsync(userId);
            if (user == null) return (false, "User not found.");
            _db.Users.Remove(user);
            await _db.SaveChangesAsync();
            return (true, $"{user.FullName} has been deleted.");
        }
    }
}