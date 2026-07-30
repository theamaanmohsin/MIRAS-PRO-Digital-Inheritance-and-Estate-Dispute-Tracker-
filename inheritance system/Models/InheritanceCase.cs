using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InheritanceSystem.Models
{
    [Table("InheritanceCases")]
    public class InheritanceCase
    {
        [Key]
        public int CaseId { get; set; }

        [Required]
        public int PropertyId { get; set; }

        [MaxLength(50)]
        public string? Status { get; set; } = "Active";   // 'Active' | 'Closed'

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation property
        public Property? Property { get; set; }
    }
}
