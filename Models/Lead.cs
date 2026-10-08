using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Models
{
    public class Lead
    {
        public int LeadId { get; set; }

        [Required(ErrorMessage = "Lead Code is required.")]
        [StringLength(50, ErrorMessage = "Lead Code cannot exceed 50 characters.")]
        public string LeadCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Lead Name is required.")]
        [StringLength(150, ErrorMessage = "Lead Name cannot exceed 150 characters.")]
        public string LeadName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [StringLength(150, ErrorMessage = "Email cannot exceed 150 characters.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone is required.")]
        [Phone(ErrorMessage = "Enter a valid phone number.")]
        [StringLength(20, ErrorMessage = "Phone cannot exceed 20 characters.")]
        public string Phone { get; set; } = string.Empty;

        [StringLength(150, ErrorMessage = "Company Name cannot exceed 150 characters.")]
        public string? CompanyName { get; set; }

        [Required(ErrorMessage = "Source is required.")]
        [StringLength(100, ErrorMessage = "Source cannot exceed 100 characters.")]
        public string Source { get; set; } = string.Empty;

        [Required(ErrorMessage = "Status is required.")]
        [StringLength(50, ErrorMessage = "Status cannot exceed 50 characters.")]
        public string Status { get; set; } = "New";

        [StringLength(50, ErrorMessage = "Priority cannot exceed 50 characters.")]
        public string? Priority { get; set; } = "Medium";

        [Range(0, 999999999999.99, ErrorMessage = "Expected Value must be a non-negative number.")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal ExpectedValue { get; set; } = 0m;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [StringLength(450)]
        public string? AssignedTo { get; set; }

        public int? ConvertedCustomerId { get; set; }
        public int? ConvertedOpportunityId { get; set; }

        // Navigation properties
        public virtual ApplicationUser? AssignedUser { get; set; }
        public virtual Customer? ConvertedCustomer { get; set; }
        public virtual Opportunity? ConvertedOpportunity { get; set; }
        public virtual ICollection<Opportunity> Opportunities { get; set; } = new List<Opportunity>();
        public virtual ICollection<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
        public virtual ICollection<Activity> Activities { get; set; } = new List<Activity>();
    }
}
