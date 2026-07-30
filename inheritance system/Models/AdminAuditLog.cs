using System.ComponentModel.DataAnnotations;

namespace InheritanceSystem.Models
{
    public class AdminAuditLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int AdminUserId { get; set; }

        [Required]
        public string AdminName { get; set; } = string.Empty;

        [Required]
        public string Action { get; set; } = string.Empty;

        public string? TargetType { get; set; }
        public int? TargetId { get; set; }
        public string? Details { get; set; }
        public string? IpAddress { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string ActionBadgeClass() => Action switch
        {
            "FRC_Approved" => "badge-green",
            "FRC_Rejected" => "badge-red",
            "Token_Created" => "badge-blue",
            "Token_Revoked" => "badge-red",
            "Doc_Approved" => "badge-green",
            "Doc_Rejected" => "badge-red",
            "Doc_Deleted" => "badge-red",
            "User_Deleted" => "badge-red",
            _ => "badge-gray"
        };
    }
}