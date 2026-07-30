namespace InheritanceSystem.Models
{
    public class Dispute
    {
        public int DisputeId { get; set; }
        public int PropertyId { get; set; }
        public int FiledBy { get; set; }
        public string DisputeType { get; set; } = "";
        public string Description { get; set; } = "";
        public string Status { get; set; } = "Pending";
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>Admin explanation when status is Rejected.</summary>
        public string? AdminRejectionReason { get; set; }

        /// <summary>When true, the applicant may edit and resubmit this dispute.</summary>
        public bool AllowUserEdit { get; set; }

        public int? ReviewedByAdminId { get; set; }
        public DateTime? ReviewedAt { get; set; }

        public Property? Property { get; set; }
    }
}
