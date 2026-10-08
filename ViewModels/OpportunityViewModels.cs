using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Models;
using AcxiomCRM.Models.Constants;

namespace AcxiomCRM.ViewModels
{
    public class OpportunityListViewModel
    {
        public IEnumerable<Opportunity> Opportunities { get; set; } = new List<Opportunity>();
        public string? SearchTerm { get; set; }
        public string? StageFilter { get; set; }
        public string? StatusFilter { get; set; }
        public string? AssignedToFilter { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }

        public decimal TotalPipelineValue { get; set; }
        public decimal TotalWeightedPipelineValue { get; set; }
        public int OpenCount { get; set; }
        public int WonCount { get; set; }
        public int LostCount { get; set; }

        public Dictionary<string, string> SalesExecutives { get; set; } = new();

        public bool HasPreviousPage => PageIndex > 1;
        public bool HasNextPage => PageIndex < TotalPages;
    }

    public class OpportunityCreateViewModel : IValidatableObject
    {
        [Required(ErrorMessage = "Opportunity Name is required.")]
        [StringLength(150, ErrorMessage = "Opportunity Name cannot exceed 150 characters.")]
        [Display(Name = "Opportunity / Deal Name")]
        public string OpportunityName { get; set; } = string.Empty;

        [Display(Name = "Associated Customer")]
        public int? CustomerId { get; set; }

        [Display(Name = "Associated Lead")]
        public int? LeadId { get; set; }

        [Required(ErrorMessage = "Opportunity Amount is required.")]
        [Range(0.01, 999999999999.99, ErrorMessage = "Opportunity Amount must be greater than 0.")]
        [Display(Name = "Deal Amount (₹)")]
        public decimal Amount { get; set; } = 50000;

        [Required(ErrorMessage = "Pipeline stage is required.")]
        [Display(Name = "Pipeline Stage")]
        public string Stage { get; set; } = OpportunityStages.Qualification;

        [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
        [Display(Name = "Win Probability (%)")]
        public int Probability { get; set; } = 25;

        [Required(ErrorMessage = "Expected Close Date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Expected Close Date")]
        public DateTime ExpectedCloseDate { get; set; } = DateTime.Today.AddDays(30);

        [Display(Name = "Assign To")]
        public string? AssignedTo { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!CustomerId.HasValue && !LeadId.HasValue)
            {
                yield return new ValidationResult(
                    "You must associate either a Customer or a Lead.",
                    new[] { nameof(CustomerId), nameof(LeadId) });
            }

            if (Amount <= 0)
            {
                yield return new ValidationResult(
                    "Opportunity Amount must be greater than 0.",
                    new[] { nameof(Amount) });
            }

            if (Probability < 0 || Probability > 100)
            {
                yield return new ValidationResult(
                    "Probability must be between 0 and 100.",
                    new[] { nameof(Probability) });
            }

            if (ExpectedCloseDate.Date < DateTime.Today)
            {
                yield return new ValidationResult(
                    "Expected Close Date cannot be in the past.",
                    new[] { nameof(ExpectedCloseDate) });
            }
        }
    }

    public class OpportunityEditViewModel : IValidatableObject
    {
        public int OpportunityId { get; set; }

        [Required(ErrorMessage = "Opportunity Name is required.")]
        [StringLength(150, ErrorMessage = "Opportunity Name cannot exceed 150 characters.")]
        [Display(Name = "Opportunity / Deal Name")]
        public string OpportunityName { get; set; } = string.Empty;

        [Display(Name = "Associated Customer")]
        public int? CustomerId { get; set; }

        [Display(Name = "Associated Lead")]
        public int? LeadId { get; set; }

        [Required(ErrorMessage = "Opportunity Amount is required.")]
        [Range(0.01, 999999999999.99, ErrorMessage = "Opportunity Amount must be greater than 0.")]
        [Display(Name = "Deal Amount (₹)")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Pipeline stage is required.")]
        [Display(Name = "Pipeline Stage")]
        public string Stage { get; set; } = OpportunityStages.Qualification;

        [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
        [Display(Name = "Win Probability (%)")]
        public int Probability { get; set; }

        [Required(ErrorMessage = "Expected Close Date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Expected Close Date")]
        public DateTime ExpectedCloseDate { get; set; }

        [Required(ErrorMessage = "Status is required.")]
        [Display(Name = "Status")]
        public string Status { get; set; } = OpportunityStatuses.Open;

        [Display(Name = "Assign To")]
        public string? AssignedTo { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Status == OpportunityStatuses.Open && Amount <= 0)
            {
                yield return new ValidationResult(
                    "Opportunity Amount must be greater than 0.",
                    new[] { nameof(Amount) });
            }

            if (Probability < 0 || Probability > 100)
            {
                yield return new ValidationResult(
                    "Probability must be between 0 and 100.",
                    new[] { nameof(Probability) });
            }

            if (Status == OpportunityStatuses.Open && ExpectedCloseDate.Date < DateTime.Today)
            {
                yield return new ValidationResult(
                    "Expected Close Date cannot be in the past for active opportunities.",
                    new[] { nameof(ExpectedCloseDate) });
            }
        }
    }

    public class OpportunityDetailsViewModel
    {
        public Opportunity Opportunity { get; set; } = null!;
        public IReadOnlyList<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
        public IReadOnlyList<Activity> Activities { get; set; } = new List<Activity>();
        public IReadOnlyList<AuditLog> AuditHistory { get; set; } = new List<AuditLog>();
    }
}
