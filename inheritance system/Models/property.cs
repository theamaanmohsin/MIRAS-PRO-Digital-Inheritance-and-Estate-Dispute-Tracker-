namespace InheritanceSystem.Models
{
    public class Property
    {
        public int PropertyId { get; set; }
        public int OwnerId { get; set; }
        public string Title { get; set; } = "";
        public string PropertyType { get; set; } = "";
        public string Location { get; set; } = "";
        public decimal EstimatedValue { get; set; }
        public DateTime CreatedAt { get; set; }

        public User? Owner { get; set; }
    }
}