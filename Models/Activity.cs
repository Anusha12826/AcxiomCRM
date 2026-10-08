using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Models
{
    public class Activity
    {
        public int ActivityId { get; set; }

        [Required(ErrorMessage = "Activity Type is required.")]
        [StringLength(50, ErrorMessage = "Activity Type cannot exceed 50 characters.")]
        public string ActivityType { get; set; } = "Call";

        [Required(ErrorMessage = "Subject is required.")]
        [StringLength(200, ErrorMessage = "Subject cannot exceed 200 characters.")]
        public string Subject { get; set; } = string.Empty;

        [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters.")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Activity Date is required.")]
        public DateTime ActivityDate { get; set; }

        public int? CustomerId { get; set; }
        public int? LeadId { get; set; }

        [StringLength(450)]
        public string? AssignedTo { get; set; }

        [Required(ErrorMessage = "Status is required.")]
        [StringLength(50, ErrorMessage = "Status cannot exceed 50 characters.")]
        public string Status { get; set; } = "Pending";

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Customer? Customer { get; set; }
        public virtual Lead? Lead { get; set; }
        public virtual ApplicationUser? AssignedUser { get; set; }
    }
}
