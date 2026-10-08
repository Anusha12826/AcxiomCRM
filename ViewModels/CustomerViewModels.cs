using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Models;

namespace AcxiomCRM.ViewModels
{
    public class CustomerListViewModel
    {
        public List<Customer> Customers { get; set; } = new();
        public string? SearchTerm { get; set; }
        public string? StatusFilter { get; set; }
        public string? AssignedToFilter { get; set; }

        public int PageIndex { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalCount { get; set; }
        public int PageSize { get; set; } = 10;

        public bool HasPreviousPage => PageIndex > 1;
        public bool HasNextPage => PageIndex < TotalPages;

        public Dictionary<string, string> SalesExecutives { get; set; } = new();
    }

    public class CustomerCreateViewModel
    {
        [Display(Name = "Customer Code")]
        [StringLength(50, ErrorMessage = "Customer Code cannot exceed 50 characters.")]
        public string? CustomerCode { get; set; }

        [Required(ErrorMessage = "Customer Name is required.")]
        [StringLength(150, ErrorMessage = "Customer Name cannot exceed 150 characters.")]
        [Display(Name = "Customer Name")]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [StringLength(150, ErrorMessage = "Email cannot exceed 150 characters.")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone is required.")]
        [RegularExpression(@"^[0-9\+\-\s\(\)]{7,20}$", ErrorMessage = "Enter a valid phone number (minimum 7 digits).")]
        [StringLength(20, ErrorMessage = "Phone cannot exceed 20 characters.")]
        [Display(Name = "Phone Number")]
        public string Phone { get; set; } = string.Empty;

        [StringLength(150, ErrorMessage = "Company Name cannot exceed 150 characters.")]
        [Display(Name = "Company Name")]
        public string? CompanyName { get; set; }

        [StringLength(250, ErrorMessage = "Address cannot exceed 250 characters.")]
        public string? Address { get; set; }

        [StringLength(100, ErrorMessage = "City cannot exceed 100 characters.")]
        public string? City { get; set; }

        [StringLength(100, ErrorMessage = "State cannot exceed 100 characters.")]
        public string? State { get; set; }

        [Display(Name = "Assign Sales Executive")]
        public string? AssignedTo { get; set; }
    }

    public class CustomerEditViewModel
    {
        [Required]
        public int CustomerId { get; set; }

        [Required(ErrorMessage = "Customer Code is required.")]
        [StringLength(50, ErrorMessage = "Customer Code cannot exceed 50 characters.")]
        [Display(Name = "Customer Code")]
        public string CustomerCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Customer Name is required.")]
        [StringLength(150, ErrorMessage = "Customer Name cannot exceed 150 characters.")]
        [Display(Name = "Customer Name")]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [StringLength(150, ErrorMessage = "Email cannot exceed 150 characters.")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone is required.")]
        [RegularExpression(@"^[0-9\+\-\s\(\)]{7,20}$", ErrorMessage = "Enter a valid phone number (minimum 7 digits).")]
        [StringLength(20, ErrorMessage = "Phone cannot exceed 20 characters.")]
        [Display(Name = "Phone Number")]
        public string Phone { get; set; } = string.Empty;

        [StringLength(150, ErrorMessage = "Company Name cannot exceed 150 characters.")]
        [Display(Name = "Company Name")]
        public string? CompanyName { get; set; }

        [StringLength(250, ErrorMessage = "Address cannot exceed 250 characters.")]
        public string? Address { get; set; }

        [StringLength(100, ErrorMessage = "City cannot exceed 100 characters.")]
        public string? City { get; set; }

        [StringLength(100, ErrorMessage = "State cannot exceed 100 characters.")]
        public string? State { get; set; }

        [Required(ErrorMessage = "Status is required.")]
        [Display(Name = "Status")]
        public string Status { get; set; } = "Active";

        [Display(Name = "Assign Sales Executive")]
        public string? AssignedTo { get; set; }
    }

    public class CustomerDetailsViewModel
    {
        public Customer Customer { get; set; } = null!;
        public List<Opportunity> Opportunities { get; set; } = new();
        public List<FollowUp> FollowUps { get; set; } = new();
        public List<Activity> Activities { get; set; } = new();
        public List<AuditLog> AuditHistory { get; set; } = new();
    }
}
