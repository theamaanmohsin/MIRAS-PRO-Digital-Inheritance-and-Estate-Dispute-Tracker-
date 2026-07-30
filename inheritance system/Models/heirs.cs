namespace InheritanceSystem.Models
{
    public class Heir
    {
        public int HeirId { get; set; }
        public int PropertyId { get; set; }
        public string FullName { get; set; } = "";
        public string Relation { get; set; } = "";
        public decimal SharePercent { get; set; }
        public DateTime CreatedAt { get; set; }

        public Property? Property { get; set; }
    }
}