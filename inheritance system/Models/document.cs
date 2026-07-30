namespace InheritanceSystem.Models
{
    public class Document
    {
        public int DocumentId { get; set; }
        public int PropertyId { get; set; }
        public int UploadedBy { get; set; }
        public string FileName { get; set; } = "";
        public string FilePath { get; set; } = "";
        public string Status { get; set; } = "Pending";   // Pending | Approved | Rejected
        public string? AdminNotes { get; set; }
        public int? ReviewedByAdminId { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public DateTime UploadedAt { get; set; }

        public Property? Property { get; set; }
    }
}