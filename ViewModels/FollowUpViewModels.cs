using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Models;
using AcxiomCRM.Models.Constants;

namespace AcxiomCRM.ViewModels
{
    public class FollowUpListViewModel
    {
        public IEnumerable<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
        public string? FilterTab { get; set; } = "all"; // all, pending, overdue, completed
        public string? TypeFilter { get; set; }
        public string? AssignedToFilter { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }

        public int PendingCount { get; set; }
        public int OverdueCount { get; set; }
        public int CompletedCount { get; set; }

        public Dictionary<string, string> SalesExecutives { get; set; } = new();

        public bool HasPreviousPage => PageIndex > 1;
        public bool HasNextPage => PageIndex < TotalPages;
    }

    public class FollowUpCreateViewModel : IValidatableObject
    {
        [Display(Name = "Linked Customer")]
        public int? CustomerId { get; set; }

        [Display(Name = "Linked Lead")]
        public int? LeadId { get; set; }

        [Display(Name = "Linked Opportunity")]
        public int? OpportunityId { get; set; }

        [Required(ErrorMessage = "Follow-up date and time is required.")]
        [Display(Name = "Follow-up Date & Time")]
        public DateTime FollowUpDate { get; set; } = DateTime.Today.AddDays(1).AddHours(10);

        [Required(ErrorMessage = "Follow-up type is required.")]
        [StringLength(50)]
        [Display(Name = "Follow-up Type")]
        public string FollowUpType { get; set; } = FollowUpTypes.Call;

        [StringLength(1000, ErrorMessage = "Remarks cannot exceed 1000 characters.")]
        [Display(Name = "Remarks / Notes")]
        public string? Remarks { get; set; }

        [Display(Name = "Assign To")]
        public string? AssignedTo { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!CustomerId.HasValue && !LeadId.HasValue)
            {
                yield return new ValidationResult(
                    "You must select either an associated Customer or Lead.",
                    new[] { nameof(CustomerId), nameof(LeadId) });
            }

            if (FollowUpDate.Date < DateTime.Today)
            {
                yield return new ValidationResult(
                    "Follow-up date cannot be earlier than today.",
                    new[] { nameof(FollowUpDate) });
            }
        }
    }

    public class FollowUpEditViewModel : IValidatableObject
    {
        public int FollowUpId { get; set; }

        [Display(Name = "Linked Customer")]
        public int? CustomerId { get; set; }

        [Display(Name = "Linked Lead")]
        public int? LeadId { get; set; }

        [Display(Name = "Linked Opportunity")]
        public int? OpportunityId { get; set; }

        [Required(ErrorMessage = "Follow-up date is required.")]
        [Display(Name = "Follow-up Date & Time")]
        public DateTime FollowUpDate { get; set; }

        [Required(ErrorMessage = "Follow-up type is required.")]
        [StringLength(50)]
        [Display(Name = "Follow-up Type")]
        public string FollowUpType { get; set; } = FollowUpTypes.Call;

        [Required(ErrorMessage = "Status is required.")]
        [Display(Name = "Status")]
        public string Status { get; set; } = FollowUpStatuses.Planned;

        [StringLength(1000, ErrorMessage = "Remarks cannot exceed 1000 characters.")]
        [Display(Name = "Remarks / Notes")]
        public string? Remarks { get; set; }

        [Display(Name = "Assign To")]
        public string? AssignedTo { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Status == FollowUpStatuses.Planned && FollowUpDate.Date < DateTime.Today)
            {
                yield return new ValidationResult(
                    "Follow-up date cannot be earlier than today for planned follow-ups.",
                    new[] { nameof(FollowUpDate) });
            }
        }
    }

    public class FollowUpRescheduleViewModel
    {
        public int FollowUpId { get; set; }

        [Required(ErrorMessage = "New follow-up date is required.")]
        [Display(Name = "New Follow-up Date & Time")]
        public DateTime NewFollowUpDate { get; set; } = DateTime.Today.AddDays(1).AddHours(10);

        [StringLength(500, ErrorMessage = "Reason cannot exceed 500 characters.")]
        [Display(Name = "Reschedule Reason / Remarks")]
        public string? RescheduleReason { get; set; }
    }

    public class FollowUpCompleteViewModel
    {
        public int FollowUpId { get; set; }

        [Required(ErrorMessage = "Outcome remarks are required to mark follow-up complete.")]
        [StringLength(1000, ErrorMessage = "Completion remarks cannot exceed 1000 characters.")]
        [Display(Name = "Completion Remarks / Outcome")]
        public string CompletionRemarks { get; set; } = string.Empty;
    }
}
