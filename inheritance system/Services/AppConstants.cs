namespace inheritance_system.Services
{
    public static class AppConstants
    {
        public const string RoleAdmin = "Admin";
        public const string RoleUser = "User";

        /// <summary>Default admin security key (seeded in database on first run).</summary>
        public const string DefaultAdminAccessSecret = "MirasPro@Admin2025";

        public static readonly string[] DisputeStatuses =
        {
            "Pending", "Under Review", "Approved", "Rejected"
        };

        /// <summary>Legacy role values migrated to User.</summary>
        public static readonly string[] LegacyUserRoles = { "Owner", "LegalProfessional" };
    }
}
