using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.DTOs
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public List<string>? Errors { get; set; }

        public static ApiResponse<T> Ok(T data, string message = "Success") =>
            new() { Success = true, Message = message, Data = data };

        public static ApiResponse<T> Fail(string message, List<string>? errors = null) =>
            new() { Success = false, Message = message, Errors = errors };
    }

    public class CustomerDto
    {
        public int CustomerId { get; set; }
        public string CustomerCode { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public string? AssignedTo { get; set; }
        public string? AssignedUserName { get; set; }
    }

    public class CustomerCreateDto
    {
        public string? CustomerCode { get; set; }

        [Required(ErrorMessage = "Customer Name is required.")]
        [StringLength(150)]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        [Phone(ErrorMessage = "Enter a valid phone number.")]
        [StringLength(20)]
        public string Phone { get; set; } = string.Empty;

        public string? CompanyName { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? AssignedTo { get; set; }
    }

    public class CustomerUpdateDto
    {
        [Required(ErrorMessage = "Customer Name is required.")]
        [StringLength(150)]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        [Phone(ErrorMessage = "Enter a valid phone number.")]
        [StringLength(20)]
        public string Phone { get; set; } = string.Empty;

        public string? CompanyName { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string Status { get; set; } = "Active";
        public string? AssignedTo { get; set; }
    }

    public class LeadDto
    {
        public int LeadId { get; set; }
        public string LeadCode { get; set; } = string.Empty;
        public string LeadName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
        public string Source { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? Priority { get; set; }
        public decimal ExpectedValue { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? AssignedTo { get; set; }
        public string? AssignedUserName { get; set; }
        public int? ConvertedCustomerId { get; set; }
        public int? ConvertedOpportunityId { get; set; }
    }

    public class LeadCreateDto
    {
        public string? LeadCode { get; set; }

        [Required(ErrorMessage = "Lead Name is required.")]
        [StringLength(150)]
        public string LeadName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        [Phone(ErrorMessage = "Enter a valid phone number.")]
        [StringLength(20)]
        public string Phone { get; set; } = string.Empty;

        public string? CompanyName { get; set; }

        [Required(ErrorMessage = "Source is required.")]
        public string Source { get; set; } = "Website";

        public string Priority { get; set; } = "Medium";

        [Range(0, 999999999999.99, ErrorMessage = "Expected Value must be 0 or greater.")]
        public decimal ExpectedValue { get; set; } = 0;

        public string? AssignedTo { get; set; }
    }

    public class LeadUpdateDto
    {
        [Required(ErrorMessage = "Lead Name is required.")]
        [StringLength(150)]
        public string LeadName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        [Phone(ErrorMessage = "Enter a valid phone number.")]
        [StringLength(20)]
        public string Phone { get; set; } = string.Empty;

        public string? CompanyName { get; set; }
        public string Source { get; set; } = "Website";
        public string Status { get; set; } = "New";
        public string Priority { get; set; } = "Medium";
        public decimal ExpectedValue { get; set; } = 0;
        public string? AssignedTo { get; set; }
    }

    public class LeadConvertDto
    {
        public string? CustomerName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? CompanyName { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public bool CreateOpportunity { get; set; } = true;
        public string? OpportunityName { get; set; }
        public decimal OpportunityAmount { get; set; }
        public string OpportunityStage { get; set; } = "Qualification";
        public int Probability { get; set; } = 30;
        public DateTime? ExpectedCloseDate { get; set; }
        public string? AssignedTo { get; set; }
    }

    public class LeadConvertResultDto
    {
        public int? CustomerId { get; set; }
        public int? OpportunityId { get; set; }
    }

    public class OpportunityDto
    {
        public int OpportunityId { get; set; }
        public string OpportunityName { get; set; } = string.Empty;
        public int? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public int? LeadId { get; set; }
        public string? LeadName { get; set; }
        public decimal Amount { get; set; }
        public string Stage { get; set; } = string.Empty;
        public int Probability { get; set; }
        public decimal WeightedPipeline { get; set; }
        public DateTime ExpectedCloseDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public string? AssignedTo { get; set; }
        public string? AssignedUserName { get; set; }
    }

    public class OpportunityCreateDto
    {
        [Required(ErrorMessage = "Opportunity Name is required.")]
        [StringLength(150)]
        public string OpportunityName { get; set; } = string.Empty;

        public int? CustomerId { get; set; }
        public int? LeadId { get; set; }

        [Required]
        [Range(0.01, 999999999999.99, ErrorMessage = "Opportunity Amount must be greater than 0.")]
        public decimal Amount { get; set; }

        [Required]
        public string Stage { get; set; } = "Qualification";

        [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
        public int Probability { get; set; } = 25;

        [Required]
        public DateTime ExpectedCloseDate { get; set; }

        public string? AssignedTo { get; set; }
    }

    public class OpportunityUpdateDto
    {
        [Required(ErrorMessage = "Opportunity Name is required.")]
        [StringLength(150)]
        public string OpportunityName { get; set; } = string.Empty;

        public int? CustomerId { get; set; }
        public int? LeadId { get; set; }

        [Required]
        [Range(0.01, 999999999999.99, ErrorMessage = "Opportunity Amount must be greater than 0.")]
        public decimal Amount { get; set; }

        [Required]
        public string Stage { get; set; } = "Qualification";

        [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
        public int Probability { get; set; }

        [Required]
        public DateTime ExpectedCloseDate { get; set; }

        public string Status { get; set; } = "Open";

        public string? AssignedTo { get; set; }
    }
}
