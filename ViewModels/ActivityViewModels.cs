using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Models;
using AcxiomCRM.Models.Constants;

namespace AcxiomCRM.ViewModels
{
    public class ActivityListViewModel
    {
        public IEnumerable<Activity> Activities { get; set; } = new List<Activity>();
        public string? SearchTerm { get; set; }
        public string? TypeFilter { get; set; }
        public string? StatusFilter { get; set; }
        public string? AssignedToFilter { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }

        public int CallCount { get; set; }
        public int MeetingCount { get; set; }
        public int EmailCount { get; set; }
        public int TaskCount { get; set; }

        public Dictionary<string, string> SalesExecutives { get; set; } = new();

        public bool HasPreviousPage => PageIndex > 1;
        public bool HasNextPage => PageIndex < TotalPages;
    }

    public class ActivityCreateViewModel : IValidatableObject
    {
        [Required(ErrorMessage = "Activity Type is required.")]
        [Display(Name = "Activity Type")]
        public string ActivityType { get; set; } = ActivityTypes.Call;

        [Required(ErrorMessage = "Subject is required.")]
        [StringLength(200, ErrorMessage = "Subject cannot exceed 200 characters.")]
        [Display(Name = "Activity Subject")]
        public string Subject { get; set; } = string.Empty;

        [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters.")]
        [Display(Name = "Description / Summary")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Activity Date is required.")]
        [Display(Name = "Activity Date & Time")]
        public DateTime ActivityDate { get; set; } = DateTime.UtcNow;

        [Display(Name = "Associated Customer")]
        public int? CustomerId { get; set; }

        [Display(Name = "Associated Lead")]
        public int? LeadId { get; set; }

        [Required(ErrorMessage = "Status is required.")]
        [Display(Name = "Status")]
        public string Status { get; set; } = "Completed";

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
        }
    }

    public class ActivityEditViewModel : IValidatableObject
    {
        public int ActivityId { get; set; }

        [Required(ErrorMessage = "Activity Type is required.")]
        [Display(Name = "Activity Type")]
        public string ActivityType { get; set; } = ActivityTypes.Call;

        [Required(ErrorMessage = "Subject is required.")]
        [StringLength(200, ErrorMessage = "Subject cannot exceed 200 characters.")]
        [Display(Name = "Activity Subject")]
        public string Subject { get; set; } = string.Empty;

        [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters.")]
        [Display(Name = "Description / Summary")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Activity Date is required.")]
        [Display(Name = "Activity Date & Time")]
        public DateTime ActivityDate { get; set; }

        [Display(Name = "Associated Customer")]
        public int? CustomerId { get; set; }

        [Display(Name = "Associated Lead")]
        public int? LeadId { get; set; }

        [Required(ErrorMessage = "Status is required.")]
        [Display(Name = "Status")]
        public string Status { get; set; } = "Completed";

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
        }
    }
}
