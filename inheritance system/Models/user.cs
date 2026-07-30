namespace InheritanceSystem.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string PasswordHash { get; set; } = "";
        public string Role { get; set; } = "";
        public string? Phone { get; set; }
        public string? Cnic { get; set; }
        public string? BarCouncilNumber { get; set; }
        public string? FirmName { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsEditable { get; set; } = true;
    }
}
