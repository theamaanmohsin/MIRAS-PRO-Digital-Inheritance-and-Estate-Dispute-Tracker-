using Microsoft.AspNetCore.Http;

namespace inheritance_system.Services
{
    public class CurrentUserService
    {
        private readonly Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage.ProtectedSessionStorage _sessionStorage;

        private const string KeyUserId = "miraspro_user_id";
        private const string KeyFullName = "miraspro_full_name";
        private const string KeyEmail = "miraspro_email";
        private const string KeyRole = "miraspro_role";

        public int UserId { get; private set; }
        public string FullName { get; private set; } = "";
        public string Email { get; private set; } = "";
        public string Role { get; private set; } = "";
        public bool IsLoggedIn => UserId > 0;
        public bool IsAdmin => Role == AppConstants.RoleAdmin;
        public bool IsUser => UserService.IsUserRole(Role);

        /// <summary>Only administrators may edit or delete existing records.</summary>
        public bool CanModifyRecords => IsAdmin;

        public CurrentUserService(Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage.ProtectedSessionStorage sessionStorage)
        {
            _sessionStorage = sessionStorage;
        }

        public async Task<CurrentUserResult> GetCurrentUser()
        {
            if (!IsLoggedIn)
                await LoadFromSessionAsync();

            if (IsLoggedIn)
            {
                return new CurrentUserResult
                {
                    ok = true,
                    msg = "User session loaded successfully.",
                    UserId = UserId,
                    FullName = FullName,
                    Email = Email,
                    Role = Role
                };
            }

            return new CurrentUserResult { ok = false, msg = "No active user session found." };
        }

        public async Task SetUserAsync(int userId, string fullName, string email, string role)
        {
            UserId = userId;
            FullName = fullName;
            Email = email;
            Role = role;

            try {
                await _sessionStorage.SetAsync(KeyUserId, userId);
                await _sessionStorage.SetAsync(KeyFullName, fullName);
                await _sessionStorage.SetAsync(KeyEmail, email);
                await _sessionStorage.SetAsync(KeyRole, role);
            } catch { }
        }

        public async Task<bool> LoadFromSessionAsync()
        {
            if (IsLoggedIn)
                return true;

            try
            {
                var idResult = await _sessionStorage.GetAsync<int>(KeyUserId);
                if (!idResult.Success || idResult.Value <= 0)
                    return false;

                UserId = idResult.Value;
                FullName = (await _sessionStorage.GetAsync<string>(KeyFullName)).Value ?? "";
                Email = (await _sessionStorage.GetAsync<string>(KeyEmail)).Value ?? "";
                Role = (await _sessionStorage.GetAsync<string>(KeyRole)).Value ?? "";
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task LogoutAsync()
        {
            UserId = 0;
            FullName = "";
            Email = "";
            Role = "";

            try {
                await _sessionStorage.DeleteAsync(KeyUserId);
                await _sessionStorage.DeleteAsync(KeyFullName);
                await _sessionStorage.DeleteAsync(KeyEmail);
                await _sessionStorage.DeleteAsync(KeyRole);
            } catch { }
        }


    }

    public class CurrentUserResult
    {
        public bool ok { get; set; }
        public string msg { get; set; } = "";
        public int UserId { get; set; }
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Role { get; set; } = "";

        public void Deconstruct(out bool ok, out string msg)
        {
            ok = this.ok;
            msg = this.msg;
        }
    }
}
