using AcxiomCRM.Models;

namespace AcxiomCRM.ViewModels
{
    public class DashboardViewModel
    {
        // User & Role Context
        public string UserName { get; set; } = string.Empty;
        public string UserFullName { get; set; } = string.Empty;
        public string UserRole { get; set; } = string.Empty;

        // Date Filtering
        public string DateFilter { get; set; } = "All"; // All, Today, ThisWeek, ThisMonth, Custom
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        // Mandatory KPI Summary Cards
        public int TotalCustomers { get; set; }
        public int TotalLeads { get; set; }
        public int OpenLeads { get; set; }
        public int TotalOpportunities { get; set; }
        public int OpenOpportunities { get; set; }
        public int WonOpportunities { get; set; }
        public int LostOpportunities { get; set; }
        public decimal TotalPipelineValue { get; set; }
        public decimal WeightedPipelineValue { get; set; }

        // Follow-Up Metrics
        public int PendingFollowUpsCount { get; set; }
        public int OverdueFollowUpsCount { get; set; }

        // Recent Lists
        public List<FollowUp> UpcomingFollowUps { get; set; } = new();
        public List<Activity> RecentActivities { get; set; } = new();

        // Chart Data (Real DB Data)
        public List<string> LeadStatusLabels { get; set; } = new();
        public List<int> LeadStatusCounts { get; set; } = new();

        public List<string> PipelineStageLabels { get; set; } = new();
        public List<decimal> PipelineStageAmounts { get; set; } = new();

        public List<string> MonthlySalesLabels { get; set; } = new();
        public List<decimal> MonthlySalesWon { get; set; } = new();
        public List<decimal> MonthlySalesPipeline { get; set; } = new();
    }
}
