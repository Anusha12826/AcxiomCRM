using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Models
{
    public class FollowUp
    {
        public int FollowUpId { get; set; }

        public int? CustomerId { get; set; }
        public int? LeadId { get; set; }
        public int? OpportunityId { get; set; }

        [Required(ErrorMessage = "Follow-up Date is required.")]
        public DateTime FollowUpDate { get; set; }

        [Required(ErrorMessage = "Follow-up Type is required.")]
        [StringLength(50, ErrorMessage = "Follow-up Type cannot exceed 50 characters.")]
        public string FollowUpType { get; set; } = "Call";

        [StringLength(1000, ErrorMessage = "Remarks cannot exceed 1000 characters.")]
        public string? Remarks { get; set; }

        [Required(ErrorMessage = "Status is required.")]
        [StringLength(50, ErrorMessage = "Status cannot exceed 50 characters.")]
        public string Status { get; set; } = "Planned";

        [StringLength(450)]
        public string? AssignedTo { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Customer? Customer { get; set; }
        public virtual Lead? Lead { get; set; }
        public virtual Opportunity? Opportunity { get; set; }
        public virtual ApplicationUser? AssignedUser { get; set; }
    }
}
