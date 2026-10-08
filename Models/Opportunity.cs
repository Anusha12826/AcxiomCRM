using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Models
{
    public class Opportunity
    {
        public int OpportunityId { get; set; }

        [Required(ErrorMessage = "Opportunity Name is required.")]
        [StringLength(150, ErrorMessage = "Opportunity Name cannot exceed 150 characters.")]
        public string OpportunityName { get; set; } = string.Empty;

        public int? CustomerId { get; set; }
        public int? LeadId { get; set; }

        [Range(0.01, 999999999999.99, ErrorMessage = "Opportunity Amount must be greater than 0.")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Stage is required.")]
        [StringLength(50, ErrorMessage = "Stage cannot exceed 50 characters.")]
        public string Stage { get; set; } = "Qualification";

        [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
        public int Probability { get; set; } = 10;

        [Required(ErrorMessage = "Expected Close Date is required.")]
        [DataType(DataType.Date)]
        public DateTime ExpectedCloseDate { get; set; }

        [Required(ErrorMessage = "Status is required.")]
        [StringLength(50, ErrorMessage = "Status cannot exceed 50 characters.")]
        public string Status { get; set; } = "Open";

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [StringLength(450)]
        public string? AssignedTo { get; set; }

        [NotMapped]
        public decimal WeightedPipeline => Amount * Probability / 100m;

        // Navigation properties
        public virtual Customer? Customer { get; set; }
        public virtual Lead? Lead { get; set; }
        public virtual ApplicationUser? AssignedUser { get; set; }
        public virtual ICollection<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
    }
}
