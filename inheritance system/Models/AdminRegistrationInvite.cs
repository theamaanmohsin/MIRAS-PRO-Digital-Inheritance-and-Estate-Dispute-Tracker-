using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InheritanceSystem.Models
{
    public class AdminRegistrationInvite
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string SecureInviteToken { get; set; } = string.Empty;

        [Required]
        public string RecipientEmail { get; set; } = string.Empty;

        public bool IsUsed { get; set; } = false;

        public int? CreatedByAdminId { get; set; }
        public int? UsedByUserId { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UsedAt { get; set; }

        // ── Helpers ───────────────────────────────────────────────
        public bool IsExpired() => DateTime.Now > ExpiresAt;
        public bool IsValid() => !IsUsed && !IsExpired();

        public string StatusLabel() =>
            IsUsed ? "Used" : IsExpired() ? "Expired" : "Active";

        public string StatusBadgeClass() =>
            IsUsed ? "badge-gray" : IsExpired() ? "badge-red" : "badge-green";

        public string TimeRemainingDisplay()
        {
            if (!IsValid()) return "—";
            var span = ExpiresAt - DateTime.Now;
            if (span.TotalHours < 1) return $"{(int)span.TotalMinutes}m left";
            if (span.TotalDays < 1) return $"{(int)span.TotalHours}h left";
            return $"{(int)span.TotalDays}d left";
        }
    }
}