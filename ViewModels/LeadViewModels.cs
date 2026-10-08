using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Models;
using AcxiomCRM.Models.Constants;

namespace AcxiomCRM.ViewModels
{
    public class LeadListViewModel
    {
        public IEnumerable<Lead> Leads { get; set; } = new List<Lead>();
        public string? SearchTerm { get; set; }
        public string? StatusFilter { get; set; }
        public string? PriorityFilter { get; set; }
        public string? AssignedToFilter { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public Dictionary<string, string> SalesExecutives { get; set; } = new();

        public bool HasPreviousPage => PageIndex > 1;
        public bool HasNextPage => PageIndex < TotalPages;
    }

    public class LeadCreateViewModel
    {
        [Display(Name = "Lead Code")]
        public string? LeadCode { get; set; }

        [Required(ErrorMessage = "Lead Name is required.")]
        [StringLength(150, ErrorMessage = "Lead Name cannot exceed 150 characters.")]
        [Display(Name = "Lead / Contact Name")]
        public string LeadName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [StringLength(150, ErrorMessage = "Email cannot exceed 150 characters.")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        [Phone(ErrorMessage = "Enter a valid phone number.")]
        [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters.")]
        [Display(Name = "Phone Number")]
        public string Phone { get; set; } = string.Empty;

        [StringLength(150, ErrorMessage = "Company Name cannot exceed 150 characters.")]
        [Display(Name = "Company Name")]
        public string? CompanyName { get; set; }

        [Required(ErrorMessage = "Lead Source is required.")]
        [StringLength(100, ErrorMessage = "Source cannot exceed 100 characters.")]
        [Display(Name = "Lead Source")]
        public string Source { get; set; } = "Website";

        [Required(ErrorMessage = "Priority is required.")]
        [Display(Name = "Priority")]
        public string Priority { get; set; } = "Medium";

        [Required(ErrorMessage = "Expected Value is required.")]
        [Range(0, 999999999999.99, ErrorMessage = "Expected Value must be 0 or greater.")]
        [Display(Name = "Expected Value (₹)")]
        public decimal ExpectedValue { get; set; } = 0;

        [Display(Name = "Assigned To")]
        public string? AssignedTo { get; set; }
    }

    public class LeadEditViewModel
    {
        public int LeadId { get; set; }

        [Display(Name = "Lead Code")]
        public string LeadCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Lead Name is required.")]
        [StringLength(150, ErrorMessage = "Lead Name cannot exceed 150 characters.")]
        [Display(Name = "Lead / Contact Name")]
        public string LeadName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [StringLength(150, ErrorMessage = "Email cannot exceed 150 characters.")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        [Phone(ErrorMessage = "Enter a valid phone number.")]
        [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters.")]
        [Display(Name = "Phone Number")]
        public string Phone { get; set; } = string.Empty;

        [StringLength(150, ErrorMessage = "Company Name cannot exceed 150 characters.")]
        [Display(Name = "Company Name")]
        public string? CompanyName { get; set; }

        [Required(ErrorMessage = "Lead Source is required.")]
        [StringLength(100, ErrorMessage = "Source cannot exceed 100 characters.")]
        [Display(Name = "Lead Source")]
        public string Source { get; set; } = "Website";

        [Required(ErrorMessage = "Status is required.")]
        [Display(Name = "Status")]
        public string Status { get; set; } = LeadStatuses.New;

        [Required(ErrorMessage = "Priority is required.")]
        [Display(Name = "Priority")]
        public string Priority { get; set; } = "Medium";

        [Required(ErrorMessage = "Expected Value is required.")]
        [Range(0, 999999999999.99, ErrorMessage = "Expected Value must be 0 or greater.")]
        [Display(Name = "Expected Value (₹)")]
        public decimal ExpectedValue { get; set; } = 0;

        [Display(Name = "Assigned To")]
        public string? AssignedTo { get; set; }

        public bool IsConverted => Status == LeadStatuses.Converted;
    }

    public class LeadDetailsViewModel
    {
        public Lead Lead { get; set; } = null!;
        public IReadOnlyList<Opportunity> Opportunities { get; set; } = new List<Opportunity>();
        public IReadOnlyList<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
        public IReadOnlyList<Activity> Activities { get; set; } = new List<Activity>();
        public IReadOnlyList<AuditLog> AuditHistory { get; set; } = new List<AuditLog>();
    }

    public class LeadConvertViewModel
    {
        public int LeadId { get; set; }

        [Display(Name = "Lead Code")]
        public string LeadCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Customer Name is required.")]
        [StringLength(150, ErrorMessage = "Customer Name cannot exceed 150 characters.")]
        [Display(Name = "Customer Name")]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [StringLength(150, ErrorMessage = "Email cannot exceed 150 characters.")]
        [Display(Name = "Customer Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        [Phone(ErrorMessage = "Enter a valid phone number.")]
        [StringLength(20, ErrorMessage = "Phone cannot exceed 20 characters.")]
        [Display(Name = "Customer Phone")]
        public string Phone { get; set; } = string.Empty;

        [StringLength(150, ErrorMessage = "Company Name cannot exceed 150 characters.")]
        [Display(Name = "Company Name")]
        public string? CompanyName { get; set; }

        [StringLength(250, ErrorMessage = "Address cannot exceed 250 characters.")]
        [Display(Name = "Address")]
        public string? Address { get; set; }

        [StringLength(100, ErrorMessage = "City cannot exceed 100 characters.")]
        [Display(Name = "City")]
        public string? City { get; set; }

        [StringLength(100, ErrorMessage = "State cannot exceed 100 characters.")]
        [Display(Name = "State")]
        public string? State { get; set; }

        [Display(Name = "Create Opportunity along with Customer?")]
        public bool CreateOpportunity { get; set; } = true;

        [StringLength(150, ErrorMessage = "Opportunity Name cannot exceed 150 characters.")]
        [Display(Name = "Opportunity Name")]
        public string? OpportunityName { get; set; }

        [Range(0.01, 999999999999.99, ErrorMessage = "Opportunity Amount must be greater than 0.")]
        [Display(Name = "Opportunity Amount (₹)")]
        public decimal OpportunityAmount { get; set; }

        [Display(Name = "Pipeline Stage")]
        public string OpportunityStage { get; set; } = OpportunityStages.Qualification;

        [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
        [Display(Name = "Probability (%)")]
        public int Probability { get; set; } = 30;

        [Display(Name = "Expected Close Date")]
        public DateTime ExpectedCloseDate { get; set; } = DateTime.Today.AddDays(30);

        [Display(Name = "Assign To")]
        public string? AssignedTo { get; set; }
    }
}
