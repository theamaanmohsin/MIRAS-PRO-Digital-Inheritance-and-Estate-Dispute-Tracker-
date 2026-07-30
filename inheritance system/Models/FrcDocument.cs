using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InheritanceSystem.Models
{
    public class FrcDocument
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        [Required]
        public string FrcNumber { get; set; } = string.Empty;

        [Required]
        public string DocumentFilePath { get; set; } = string.Empty;

        public string? OriginalFileName { get; set; }
        public long? FileSizeBytes { get; set; }

        // Pending | Approved | Rejected
        public string Status { get; set; } = "Pending";

        public string? AdminNotes { get; set; }
        public int? ReviewedByAdminId { get; set; }

        [ForeignKey("ReviewedByAdminId")]
        public User? ReviewedByAdmin { get; set; }
        public DateTime? ReviewedAt { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // ── Helpers ───────────────────────────────────────────────
        public bool IsPending() => Status == "Pending";
        public bool IsApproved() => Status == "Approved";
        public bool IsRejected() => Status == "Rejected";

        public string StatusBadgeClass() => Status switch
        {
            "Approved" => "badge-green",
            "Rejected" => "badge-red",
            _ => "badge-amber"
        };

        public string FileSizeDisplay()
        {
            if (FileSizeBytes == null) return "—";
            if (FileSizeBytes < 1024) return $"{FileSizeBytes} B";
            if (FileSizeBytes < 1_048_576) return $"{FileSizeBytes / 1024.0:N1} KB";
            return $"{FileSizeBytes / 1_048_576.0:N1} MB";
        }
    }
}