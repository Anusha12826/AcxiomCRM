using AcxiomCRM.Models;
using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.ViewModels
{
    public class ReportFilterViewModel
    {
        [Display(Name = "From Date")]
        [DataType(DataType.Date)]
        public DateTime? FromDate { get; set; }

        [Display(Name = "To Date")]
        [DataType(DataType.Date)]
        public DateTime? ToDate { get; set; }

        [Display(Name = "Status")]
        public string? Status { get; set; }

        [Display(Name = "Stage")]
        public string? Stage { get; set; }

        [Display(Name = "Assigned User")]
        public string? AssignedTo { get; set; }

        public string ReportType { get; set; } = "Customers";
    }

    public class CustomerReportItem
    {
        public string CustomerCode { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
        public string? City { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? OwnerName { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class LeadReportItem
    {
        public string LeadCode { get; set; } = string.Empty;
        public string LeadName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
        public string Source { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public decimal ExpectedValue { get; set; }
        public string? OwnerName { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsConverted { get; set; }
        public int? ConvertedCustomerId { get; set; }
    }

    public class FollowUpReportItem
    {
        public int FollowUpId { get; set; }
        public string RelatedEntityName { get; set; } = string.Empty;
        public string FollowUpType { get; set; } = string.Empty;
        public DateTime FollowUpDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsOverdue { get; set; }
        public string? OwnerName { get; set; }
        public string? Remarks { get; set; }
    }

    public class OpportunityReportItem
    {
        public int OpportunityId { get; set; }
        public string OpportunityName { get; set; } = string.Empty;
        public string? CustomerOrLeadName { get; set; }
        public string Stage { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int Probability { get; set; }
        public decimal WeightedAmount => (Amount * Probability) / 100m;
        public DateTime ExpectedCloseDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? OwnerName { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class PipelineReportViewModel
    {
        public ReportFilterViewModel Filter { get; set; } = new ReportFilterViewModel();
        public decimal TotalPipelineValue { get; set; }
        public decimal TotalWeightedValue { get; set; }
        public int TotalOpportunitiesCount { get; set; }
        public List<PipelineStageSummaryItem> StageSummaries { get; set; } = new List<PipelineStageSummaryItem>();
        public List<PipelineOwnerSummaryItem> OwnerSummaries { get; set; } = new List<PipelineOwnerSummaryItem>();
        public List<OpportunityReportItem> Opportunities { get; set; } = new List<OpportunityReportItem>();
    }

    public class PipelineStageSummaryItem
    {
        public string Stage { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal WeightedAmount { get; set; }
        public double PercentageOfPipeline { get; set; }
    }

    public class PipelineOwnerSummaryItem
    {
        public string OwnerId { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal WeightedAmount { get; set; }
    }

    public class ConversionReportViewModel
    {
        public ReportFilterViewModel Filter { get; set; } = new ReportFilterViewModel();
        public int TotalLeads { get; set; }
        public int ConvertedLeads { get; set; }
        public int LostLeads { get; set; }
        public int OpenLeads { get; set; }
        public double LeadConversionRate => TotalLeads > 0 ? Math.Round((double)ConvertedLeads / TotalLeads * 100.0, 1) : 0.0;
        public int TotalWonOpportunities { get; set; }
        public int TotalLostOpportunities { get; set; }
        public decimal TotalWonRevenue { get; set; }
        public List<LeadReportItem> RecentConvertedLeads { get; set; } = new List<LeadReportItem>();
    }

    public class UserActivityReportViewModel
    {
        public ReportFilterViewModel Filter { get; set; } = new ReportFilterViewModel();
        public List<UserActivitySummaryItem> UserSummaries { get; set; } = new List<UserActivitySummaryItem>();
        public List<ActivityReportItem> DetailedActivities { get; set; } = new List<ActivityReportItem>();
    }

    public class UserActivitySummaryItem
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int CallCount { get; set; }
        public int MeetingCount { get; set; }
        public int EmailCount { get; set; }
        public int TaskCount { get; set; }
        public int CompletedCount { get; set; }
        public int TotalActivities => CallCount + MeetingCount + EmailCount + TaskCount;
    }

    public class ActivityReportItem
    {
        public int ActivityId { get; set; }
        public string ActivityType { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string? RelatedTo { get; set; }
        public DateTime ActivityDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? OwnerName { get; set; }
    }

    public class AuditReportItem
    {
        public long AuditLogId { get; set; }
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public string? UserEmail { get; set; }
        public string Action { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public string? RecordId { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? IpAddress { get; set; }
        public string? Result { get; set; }
        public string? Details { get; set; }
    }

    public class AuditReportViewModel
    {
        public ReportFilterViewModel Filter { get; set; } = new ReportFilterViewModel();
        public List<AuditReportItem> Logs { get; set; } = new List<AuditReportItem>();
        public int TotalEvents { get; set; }
        public int SecurityEventsCount { get; set; }
        public int BusinessEventsCount { get; set; }
    }

    public class GenericReportViewModel<T>
    {
        public ReportFilterViewModel Filter { get; set; } = new ReportFilterViewModel();
        public List<T> Items { get; set; } = new List<T>();
        public int TotalCount => Items.Count;
        public Dictionary<string, string> AvailableUsers { get; set; } = new Dictionary<string, string>();
    }
}
