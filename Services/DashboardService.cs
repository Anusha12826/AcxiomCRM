using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Models.Constants;
using AcxiomCRM.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _context;

        public DashboardService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardViewModel> GetDashboardMetricsAsync(
            string userId,
            string role,
            string dateFilter = "All",
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            // 1. Calculate Date Range Boundaries
            DateTime? filterStart = null;
            DateTime? filterEnd = null;
            var now = DateTime.UtcNow;

            switch (dateFilter?.ToLower())
            {
                case "today":
                    filterStart = now.Date;
                    filterEnd = now.Date.AddDays(1).AddTicks(-1);
                    break;
                case "thisweek":
                    int diff = (7 + (int)now.DayOfWeek - (int)DayOfWeek.Monday) % 7;
                    filterStart = now.Date.AddDays(-1 * diff);
                    filterEnd = filterStart.Value.AddDays(7).AddTicks(-1);
                    break;
                case "thismonth":
                    filterStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                    filterEnd = filterStart.Value.AddMonths(1).AddTicks(-1);
                    break;
                case "custom":
                    if (startDate.HasValue) filterStart = DateTime.SpecifyKind(startDate.Value.Date, DateTimeKind.Utc);
                    if (endDate.HasValue) filterEnd = DateTime.SpecifyKind(endDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
                    break;
                default:
                    dateFilter = "All";
                    break;
            }

            // 2. Base Queries with Role-Based Scoping
            var customersQuery = _context.Customers.AsQueryable();
            var leadsQuery = _context.Leads.AsQueryable();
            var oppsQuery = _context.Opportunities.AsQueryable();
            var followUpsQuery = _context.FollowUps.Include(f => f.Customer).Include(f => f.Lead).AsQueryable();
            var activitiesQuery = _context.Activities.Include(a => a.Customer).Include(a => a.Lead).AsQueryable();

            if (role == AppRoles.SalesExecutive)
            {
                customersQuery = customersQuery.Where(c => c.AssignedTo == userId || c.CreatedBy == userId);
                leadsQuery = leadsQuery.Where(l => l.AssignedTo == userId);
                oppsQuery = oppsQuery.Where(o => o.AssignedTo == userId);
                followUpsQuery = followUpsQuery.Where(f => f.AssignedTo == userId);
                activitiesQuery = activitiesQuery.Where(a => a.AssignedTo == userId);
            }
            // Admin and Manager have access to complete organization / team CRM records

            // 3. Apply Date Filtering if configured
            if (filterStart.HasValue)
            {
                customersQuery = customersQuery.Where(c => c.CreatedDate >= filterStart.Value);
                leadsQuery = leadsQuery.Where(l => l.CreatedDate >= filterStart.Value);
                oppsQuery = oppsQuery.Where(o => o.CreatedDate >= filterStart.Value);
            }
            if (filterEnd.HasValue)
            {
                customersQuery = customersQuery.Where(c => c.CreatedDate <= filterEnd.Value);
                leadsQuery = leadsQuery.Where(l => l.CreatedDate <= filterEnd.Value);
                oppsQuery = oppsQuery.Where(o => o.CreatedDate <= filterEnd.Value);
            }

            // 4. Calculate Aggregate KPI Metrics
            var totalCustomers = await customersQuery.CountAsync();
            var totalLeads = await leadsQuery.CountAsync();
            var openLeads = await leadsQuery.CountAsync(l => l.Status != LeadStatuses.Converted && l.Status != LeadStatuses.Lost);

            var totalOpps = await oppsQuery.CountAsync();
            var openOpps = await oppsQuery.CountAsync(o => o.Status == OpportunityStatuses.Open);
            var wonOpps = await oppsQuery.CountAsync(o => o.Status == OpportunityStatuses.Won || o.Stage == OpportunityStages.Won);
            var lostOpps = await oppsQuery.CountAsync(o => o.Status == OpportunityStatuses.Lost || o.Stage == OpportunityStages.Lost);

            var openOppsList = await oppsQuery
                .Where(o => o.Status == OpportunityStatuses.Open)
                .Select(o => new { o.Amount, o.Probability })
                .ToListAsync();

            var totalPipelineValue = openOppsList.Sum(o => o.Amount);
            var weightedPipelineValue = openOppsList.Sum(o => o.Amount * o.Probability / 100m);

            // 5. Follow-Ups & Activities
            var pendingFollowUps = await followUpsQuery.CountAsync(f => f.Status == FollowUpStatuses.Planned && f.FollowUpDate >= now.Date);
            var overdueFollowUps = await followUpsQuery.CountAsync(f => f.Status == FollowUpStatuses.Planned && f.FollowUpDate < now.Date);

            var upcomingFollowUpsList = await followUpsQuery
                .Where(f => f.Status == FollowUpStatuses.Planned)
                .OrderBy(f => f.FollowUpDate)
                .Take(5)
                .ToListAsync();

            var recentActivitiesList = await activitiesQuery
                .OrderByDescending(a => a.ActivityDate)
                .Take(5)
                .ToListAsync();

            // 6. Lead Status Chart Series (New, Contacted, Qualified, Lost, Converted)
            var leadStatusesRequired = new[] { LeadStatuses.New, LeadStatuses.Contacted, LeadStatuses.Qualified, LeadStatuses.Lost, LeadStatuses.Converted };
            var allLeadStatuses = await leadsQuery
                .Select(l => l.Status)
                .ToListAsync();

            var leadStatusGroup = allLeadStatuses
                .GroupBy(s => s)
                .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

            var leadStatusCounts = leadStatusesRequired
                .Select(s => leadStatusGroup.TryGetValue(s, out var count) ? count : 0)
                .ToList();

            // 7. Opportunity Pipeline Stage Chart (Qualification, Proposal, Negotiation, Won, Lost)
            var pipelineStagesRequired = new[] { OpportunityStages.Qualification, OpportunityStages.Proposal, OpportunityStages.Negotiation, OpportunityStages.Won, OpportunityStages.Lost };
            var allOppStageData = await oppsQuery
                .Select(o => new { o.Stage, o.Amount, o.Status, o.CreatedDate })
                .ToListAsync();

            var oppStageGroup = allOppStageData
                .GroupBy(o => o.Stage)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount), StringComparer.OrdinalIgnoreCase);

            var pipelineStageAmounts = pipelineStagesRequired
                .Select(st => oppStageGroup.TryGetValue(st, out var amt) ? amt : 0m)
                .ToList();

            // 8. Monthly Sales Chart (Last 6 Months)
            var monthlyLabels = new List<string>();
            var monthlyWon = new List<decimal>();
            var monthlyPipeline = new List<decimal>();

            for (int i = 5; i >= 0; i--)
            {
                var monthDate = now.AddMonths(-i);
                var monthStart = new DateTime(monthDate.Year, monthDate.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                var monthEnd = monthStart.AddMonths(1).AddTicks(-1);

                monthlyLabels.Add(monthDate.ToString("MMM yyyy"));

                var wonSum = allOppStageData
                    .Where(o => (o.Status == OpportunityStatuses.Won || o.Stage.Equals(OpportunityStages.Won, StringComparison.OrdinalIgnoreCase)) && o.CreatedDate >= monthStart && o.CreatedDate <= monthEnd)
                    .Sum(o => o.Amount);

                var pipeSum = allOppStageData
                    .Where(o => o.Status == OpportunityStatuses.Open && o.CreatedDate >= monthStart && o.CreatedDate <= monthEnd)
                    .Sum(o => o.Amount);

                monthlyWon.Add(wonSum);
                monthlyPipeline.Add(pipeSum);
            }

            return new DashboardViewModel
            {
                UserRole = role,
                DateFilter = dateFilter,
                StartDate = startDate,
                EndDate = endDate,
                TotalCustomers = totalCustomers,
                TotalLeads = totalLeads,
                OpenLeads = openLeads,
                TotalOpportunities = totalOpps,
                OpenOpportunities = openOpps,
                WonOpportunities = wonOpps,
                LostOpportunities = lostOpps,
                TotalPipelineValue = totalPipelineValue,
                WeightedPipelineValue = weightedPipelineValue,
                PendingFollowUpsCount = pendingFollowUps,
                OverdueFollowUpsCount = overdueFollowUps,
                UpcomingFollowUps = upcomingFollowUpsList,
                RecentActivities = recentActivitiesList,
                LeadStatusLabels = leadStatusesRequired.ToList(),
                LeadStatusCounts = leadStatusCounts,
                PipelineStageLabels = pipelineStagesRequired.ToList(),
                PipelineStageAmounts = pipelineStageAmounts,
                MonthlySalesLabels = monthlyLabels,
                MonthlySalesWon = monthlyWon,
                MonthlySalesPipeline = monthlyPipeline
            };
        }
    }
}
