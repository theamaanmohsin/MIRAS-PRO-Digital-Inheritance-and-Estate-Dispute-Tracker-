namespace InheritanceSystem.Models
{
    public class Transfer
    {
        public int TransferId { get; set; }
        public int PropertyId { get; set; }
        public int FromUserId { get; set; }
        public int ToUserId { get; set; }
        public string TransferType { get; set; } = "";
        public string Status { get; set; } = "";
        public DateTime CreatedAt { get; set; }

        public Property? Property { get; set; }
    }
}