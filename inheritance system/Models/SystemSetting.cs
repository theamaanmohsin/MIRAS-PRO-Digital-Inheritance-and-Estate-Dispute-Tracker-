namespace InheritanceSystem.Models
{
    public class SystemSetting
    {
        public int Id { get; set; }
        public string Key { get; set; } = "";
        public string Value { get; set; } = "";
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        public int? UpdatedByAdminId { get; set; }
    }
}
