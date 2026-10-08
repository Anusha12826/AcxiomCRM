using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Models
{
    public class Customer
    {
        public int CustomerId { get; set; }

        [Required(ErrorMessage = "Customer Code is required.")]
        [StringLength(50, ErrorMessage = "Customer Code cannot exceed 50 characters.")]
        public string CustomerCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Customer Name is required.")]
        [StringLength(150, ErrorMessage = "Customer Name cannot exceed 150 characters.")]
        public string CustomerName { get; set; } = string.Empty;

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

        [StringLength(250, ErrorMessage = "Address cannot exceed 250 characters.")]
        public string? Address { get; set; }

        [StringLength(100, ErrorMessage = "City cannot exceed 100 characters.")]
        public string? City { get; set; }

        [StringLength(100, ErrorMessage = "State cannot exceed 100 characters.")]
        public string? State { get; set; }

        [Required(ErrorMessage = "Status is required.")]
        [StringLength(50, ErrorMessage = "Status cannot exceed 50 characters.")]
        public string Status { get; set; } = "Active";

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [Required]
        [StringLength(450)]
        public string CreatedBy { get; set; } = string.Empty;

        [StringLength(450)]
        public string? AssignedTo { get; set; }

        // Navigation properties
        public virtual ApplicationUser? Creator { get; set; }
        public virtual ApplicationUser? AssignedUser { get; set; }
        public virtual ICollection<Opportunity> Opportunities { get; set; } = new List<Opportunity>();
        public virtual ICollection<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
        public virtual ICollection<Activity> Activities { get; set; } = new List<Activity>();
    }
}
