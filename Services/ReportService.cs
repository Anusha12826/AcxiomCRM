using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Models.Constants;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Services
{
    public class ReportService : IReportService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReportService(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<Dictionary<string, string>> GetSalesUsersAsync()
        {
            return await _userManager.Users
                .OrderBy(u => u.FullName)
                .ToDictionaryAsync(u => u.Id, u => u.FullName ?? u.Email ?? u.UserName ?? u.Id);
        }

        public async Task<GenericReportViewModel<CustomerReportItem>> GetCustomerReportAsync(
            string userId, string role, ReportFilterViewModel filter)
        {
            var query = _context.Customers
                .Include(c => c.AssignedUser)
                .AsNoTracking();

            if (role == AppRoles.SalesExecutive)
            {
                query = query.Where(c => c.AssignedTo == userId || c.CreatedBy == userId);
            }
            else if (!string.IsNullOrEmpty(filter.AssignedTo))
            {
                query = query.Where(c => c.AssignedTo == filter.AssignedTo);
            }

            if (filter.FromDate.HasValue)
            {
                var fromUtc = filter.FromDate.Value.Date;
                query = query.Where(c => c.CreatedDate >= fromUtc);
            }

            if (filter.ToDate.HasValue)
            {
                var toUtc = filter.ToDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(c => c.CreatedDate <= toUtc);
            }

            if (!string.IsNullOrEmpty(filter.Status))
            {
                query = query.Where(c => c.Status == filter.Status);
            }

            var items = await query
                .OrderByDescending(c => c.CreatedDate)
                .Select(c => new CustomerReportItem
                {
                    CustomerCode = c.CustomerCode,
                    CustomerName = c.CustomerName,
                    Email = c.Email,
                    Phone = c.Phone,
                    CompanyName = c.CompanyName,
                    City = c.City,
                    Status = c.Status,
                    OwnerName = c.AssignedUser != null ? (c.AssignedUser.FullName ?? c.AssignedUser.Email) : "Unassigned",
                    CreatedDate = c.CreatedDate
                })
                .ToListAsync();

            return new GenericReportViewModel<CustomerReportItem>
            {
                Filter = filter,
                Items = items,
                AvailableUsers = await GetSalesUsersAsync()
            };
        }

        public async Task<GenericReportViewModel<LeadReportItem>> GetLeadReportAsync(
            string userId, string role, ReportFilterViewModel filter)
        {
            var query = _context.Leads
                .Include(l => l.AssignedUser)
                .AsNoTracking();

            if (role == AppRoles.SalesExecutive)
            {
                query = query.Where(l => l.AssignedTo == userId);
            }
            else if (!string.IsNullOrEmpty(filter.AssignedTo))
            {
                query = query.Where(l => l.AssignedTo == filter.AssignedTo);
            }

            if (filter.FromDate.HasValue)
            {
                var fromUtc = filter.FromDate.Value.Date;
                query = query.Where(l => l.CreatedDate >= fromUtc);
            }

            if (filter.ToDate.HasValue)
            {
                var toUtc = filter.ToDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(l => l.CreatedDate <= toUtc);
            }

            if (!string.IsNullOrEmpty(filter.Status))
            {
                query = query.Where(l => l.Status == filter.Status);
            }

            var items = await query
                .OrderByDescending(l => l.CreatedDate)
                .Select(l => new LeadReportItem
                {
                    LeadCode = l.LeadCode,
                    LeadName = l.LeadName,
                    Email = l.Email,
                    Phone = l.Phone,
                    CompanyName = l.CompanyName,
                    Source = l.Source,
                    Status = l.Status,
                    Priority = l.Priority,
                    ExpectedValue = l.ExpectedValue,
                    OwnerName = l.AssignedUser != null ? (l.AssignedUser.FullName ?? l.AssignedUser.Email) : "Unassigned",
                    CreatedDate = l.CreatedDate,
                    IsConverted = l.Status == LeadStatuses.Converted,
                    ConvertedCustomerId = l.ConvertedCustomerId
                })
                .ToListAsync();

            return new GenericReportViewModel<LeadReportItem>
            {
                Filter = filter,
                Items = items,
                AvailableUsers = await GetSalesUsersAsync()
            };
        }

        public async Task<GenericReportViewModel<FollowUpReportItem>> GetFollowUpReportAsync(
            string userId, string role, ReportFilterViewModel filter)
        {
            var query = _context.FollowUps
                .Include(f => f.Customer)
                .Include(f => f.Lead)
                .Include(f => f.AssignedUser)
                .AsNoTracking();

            if (role == AppRoles.SalesExecutive)
            {
                query = query.Where(f => f.AssignedTo == userId);
            }
            else if (!string.IsNullOrEmpty(filter.AssignedTo))
            {
                query = query.Where(f => f.AssignedTo == filter.AssignedTo);
            }

            if (filter.FromDate.HasValue)
            {
                var fromUtc = filter.FromDate.Value.Date;
                query = query.Where(f => f.FollowUpDate >= fromUtc);
            }

            if (filter.ToDate.HasValue)
            {
                var toUtc = filter.ToDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(f => f.FollowUpDate <= toUtc);
            }

            if (!string.IsNullOrEmpty(filter.Status))
            {
                query = query.Where(f => f.Status == filter.Status);
            }

            var now = DateTime.UtcNow;

            var items = await query
                .OrderBy(f => f.FollowUpDate)
                .Select(f => new FollowUpReportItem
                {
                    FollowUpId = f.FollowUpId,
                    RelatedEntityName = f.Customer != null ? f.Customer.CustomerName : (f.Lead != null ? f.Lead.LeadName : "CRM Record"),
                    FollowUpType = f.FollowUpType,
                    FollowUpDate = f.FollowUpDate,
                    Status = f.Status,
                    IsOverdue = f.Status == FollowUpStatuses.Planned && f.FollowUpDate < now,
                    OwnerName = f.AssignedUser != null ? (f.AssignedUser.FullName ?? f.AssignedUser.Email) : "Unassigned",
                    Remarks = f.Remarks
                })
                .ToListAsync();

            return new GenericReportViewModel<FollowUpReportItem>
            {
                Filter = filter,
                Items = items,
                AvailableUsers = await GetSalesUsersAsync()
            };
        }

        public async Task<GenericReportViewModel<OpportunityReportItem>> GetOpportunityReportAsync(
            string userId, string role, ReportFilterViewModel filter)
        {
            var query = _context.Opportunities
                .Include(o => o.Customer)
                .Include(o => o.Lead)
                .Include(o => o.AssignedUser)
                .AsNoTracking();

            if (role == AppRoles.SalesExecutive)
            {
                query = query.Where(o => o.AssignedTo == userId);
            }
            else if (!string.IsNullOrEmpty(filter.AssignedTo))
            {
                query = query.Where(o => o.AssignedTo == filter.AssignedTo);
            }

            if (filter.FromDate.HasValue)
            {
                var fromUtc = filter.FromDate.Value.Date;
                query = query.Where(o => o.CreatedDate >= fromUtc);
            }

            if (filter.ToDate.HasValue)
            {
                var toUtc = filter.ToDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(o => o.CreatedDate <= toUtc);
            }

            if (!string.IsNullOrEmpty(filter.Stage))
            {
                query = query.Where(o => o.Stage == filter.Stage);
            }

            if (!string.IsNullOrEmpty(filter.Status))
            {
                query = query.Where(o => o.Status == filter.Status);
            }

            var items = await query
                .OrderByDescending(o => o.CreatedDate)
                .Select(o => new OpportunityReportItem
                {
                    OpportunityId = o.OpportunityId,
                    OpportunityName = o.OpportunityName,
                    CustomerOrLeadName = o.Customer != null ? o.Customer.CustomerName : (o.Lead != null ? o.Lead.LeadName : "Direct Deal"),
                    Stage = o.Stage,
                    Amount = o.Amount,
                    Probability = o.Probability,
                    ExpectedCloseDate = o.ExpectedCloseDate,
                    Status = o.Status,
                    OwnerName = o.AssignedUser != null ? (o.AssignedUser.FullName ?? o.AssignedUser.Email) : "Unassigned",
                    CreatedDate = o.CreatedDate
                })
                .ToListAsync();

            return new GenericReportViewModel<OpportunityReportItem>
            {
                Filter = filter,
                Items = items,
                AvailableUsers = await GetSalesUsersAsync()
            };
        }

        public async Task<PipelineReportViewModel> GetPipelineReportAsync(
            string userId, string role, ReportFilterViewModel filter)
        {
            var query = _context.Opportunities
                .Include(o => o.Customer)
                .Include(o => o.Lead)
                .Include(o => o.AssignedUser)
                .AsNoTracking();

            if (role == AppRoles.SalesExecutive)
            {
                query = query.Where(o => o.AssignedTo == userId);
            }
            else if (!string.IsNullOrEmpty(filter.AssignedTo))
            {
                query = query.Where(o => o.AssignedTo == filter.AssignedTo);
            }

            if (filter.FromDate.HasValue)
            {
                var fromUtc = filter.FromDate.Value.Date;
                query = query.Where(o => o.CreatedDate >= fromUtc);
            }

            if (filter.ToDate.HasValue)
            {
                var toUtc = filter.ToDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(o => o.CreatedDate <= toUtc);
            }

            if (!string.IsNullOrEmpty(filter.Stage))
            {
                query = query.Where(o => o.Stage == filter.Stage);
            }

            // Fetch to memory for SQLite compatibility
            var oppList = (await query
                .OrderByDescending(o => o.CreatedDate)
                .Select(o => new OpportunityReportItem
                {
                    OpportunityId = o.OpportunityId,
                    OpportunityName = o.OpportunityName,
                    CustomerOrLeadName = o.Customer != null ? o.Customer.CustomerName : (o.Lead != null ? o.Lead.LeadName : "Deal"),
                    Stage = o.Stage,
                    Amount = o.Amount,
                    Probability = o.Probability,
                    ExpectedCloseDate = o.ExpectedCloseDate,
                    Status = o.Status,
                    OwnerName = o.AssignedUser != null ? (o.AssignedUser.FullName ?? o.AssignedUser.Email) : "Unassigned",
                    CreatedDate = o.CreatedDate
                })
                .ToListAsync())
                .OrderByDescending(o => o.Amount)
                .ToList();

            var totalAmount = oppList.Sum(o => o.Amount);
            var totalWeighted = oppList.Sum(o => o.WeightedAmount);

            // Group by Stage
            var predefinedStages = new[]
            {
                OpportunityStages.Qualification,
                OpportunityStages.Proposal,
                OpportunityStages.Negotiation,
                OpportunityStages.Won,
                OpportunityStages.Lost
            };

            var stageSummaries = predefinedStages.Select(stg =>
            {
                var stageOpps = oppList.Where(o => o.Stage == stg).ToList();
                var stgAmount = stageOpps.Sum(o => o.Amount);
                var stgWeighted = stageOpps.Sum(o => o.WeightedAmount);
                var pct = totalAmount > 0 ? Math.Round((double)(stgAmount / totalAmount) * 100.0, 1) : 0.0;

                return new PipelineStageSummaryItem
                {
                    Stage = stg,
                    Count = stageOpps.Count,
                    TotalAmount = stgAmount,
                    WeightedAmount = stgWeighted,
                    PercentageOfPipeline = pct
                };
            }).ToList();

            // Group by Owner
            var ownerSummaries = oppList
                .GroupBy(o => o.OwnerName ?? "Unassigned")
                .Select(g => new PipelineOwnerSummaryItem
                {
                    OwnerName = g.Key,
                    Count = g.Count(),
                    TotalAmount = g.Sum(o => o.Amount),
                    WeightedAmount = g.Sum(o => o.WeightedAmount)
                })
                .OrderByDescending(s => s.TotalAmount)
                .ToList();

            return new PipelineReportViewModel
            {
                Filter = filter,
                TotalPipelineValue = totalAmount,
                TotalWeightedValue = totalWeighted,
                TotalOpportunitiesCount = oppList.Count,
                StageSummaries = stageSummaries,
                OwnerSummaries = ownerSummaries,
                Opportunities = oppList
            };
        }

        public async Task<ConversionReportViewModel> GetConversionReportAsync(
            string userId, string role, ReportFilterViewModel filter)
        {
            var leadQuery = _context.Leads
                .Include(l => l.AssignedUser)
                .AsNoTracking();

            var oppQuery = _context.Opportunities
                .AsNoTracking();

            if (role == AppRoles.SalesExecutive)
            {
                leadQuery = leadQuery.Where(l => l.AssignedTo == userId);
                oppQuery = oppQuery.Where(o => o.AssignedTo == userId);
            }
            else if (!string.IsNullOrEmpty(filter.AssignedTo))
            {
                leadQuery = leadQuery.Where(l => l.AssignedTo == filter.AssignedTo);
                oppQuery = oppQuery.Where(o => o.AssignedTo == filter.AssignedTo);
            }

            if (filter.FromDate.HasValue)
            {
                var fromUtc = filter.FromDate.Value.Date;
                leadQuery = leadQuery.Where(l => l.CreatedDate >= fromUtc);
                oppQuery = oppQuery.Where(o => o.CreatedDate >= fromUtc);
            }

            if (filter.ToDate.HasValue)
            {
                var toUtc = filter.ToDate.Value.Date.AddDays(1).AddTicks(-1);
                leadQuery = leadQuery.Where(l => l.CreatedDate <= toUtc);
                oppQuery = oppQuery.Where(o => o.CreatedDate <= toUtc);
            }

            var leads = await leadQuery
                .Select(l => new LeadReportItem
                {
                    LeadCode = l.LeadCode,
                    LeadName = l.LeadName,
                    Email = l.Email,
                    Phone = l.Phone,
                    CompanyName = l.CompanyName,
                    Source = l.Source,
                    Status = l.Status,
                    Priority = l.Priority,
                    ExpectedValue = l.ExpectedValue,
                    OwnerName = l.AssignedUser != null ? (l.AssignedUser.FullName ?? l.AssignedUser.Email) : "Unassigned",
                    CreatedDate = l.CreatedDate,
                    IsConverted = l.Status == LeadStatuses.Converted,
                    ConvertedCustomerId = l.ConvertedCustomerId
                })
                .ToListAsync();

            var opps = await oppQuery
                .Select(o => new { o.Status, o.Amount })
                .ToListAsync();

            var totalLeads = leads.Count;
            var convertedLeads = leads.Count(l => l.Status == LeadStatuses.Converted);
            var lostLeads = leads.Count(l => l.Status == LeadStatuses.Lost);
            var openLeads = totalLeads - convertedLeads - lostLeads;

            var wonOpps = opps.Where(o => o.Status == OpportunityStatuses.Won).ToList();
            var lostOpps = opps.Count(o => o.Status == OpportunityStatuses.Lost);

            return new ConversionReportViewModel
            {
                Filter = filter,
                TotalLeads = totalLeads,
                ConvertedLeads = convertedLeads,
                LostLeads = lostLeads,
                OpenLeads = openLeads,
                TotalWonOpportunities = wonOpps.Count,
                TotalLostOpportunities = lostOpps,
                TotalWonRevenue = wonOpps.Sum(o => o.Amount),
                RecentConvertedLeads = leads.Where(l => l.IsConverted).OrderByDescending(l => l.CreatedDate).Take(15).ToList()
            };
        }

        public async Task<UserActivityReportViewModel> GetUserActivityReportAsync(
            string userId, string role, ReportFilterViewModel filter)
        {
            var userList = await _userManager.Users.AsNoTracking().ToListAsync();
            var actQuery = _context.Activities
                .Include(a => a.Customer)
                .Include(a => a.Lead)
                .Include(a => a.AssignedUser)
                .AsNoTracking();

            if (filter.FromDate.HasValue)
            {
                var fromUtc = filter.FromDate.Value.Date;
                actQuery = actQuery.Where(a => a.ActivityDate >= fromUtc);
            }

            if (filter.ToDate.HasValue)
            {
                var toUtc = filter.ToDate.Value.Date.AddDays(1).AddTicks(-1);
                actQuery = actQuery.Where(a => a.ActivityDate <= toUtc);
            }

            if (!string.IsNullOrEmpty(filter.AssignedTo))
            {
                actQuery = actQuery.Where(a => a.AssignedTo == filter.AssignedTo);
            }

            var activities = await actQuery
                .OrderByDescending(a => a.ActivityDate)
                .ToListAsync();

            var userSummaries = new List<UserActivitySummaryItem>();

            foreach (var user in userList)
            {
                var userRoles = await _userManager.GetRolesAsync(user);
                var primaryRole = userRoles.FirstOrDefault() ?? "User";

                var userActs = activities.Where(a => a.AssignedTo == user.Id).ToList();

                userSummaries.Add(new UserActivitySummaryItem
                {
                    UserId = user.Id,
                    UserName = user.FullName ?? user.UserName ?? user.Email,
                    Email = user.Email ?? string.Empty,
                    Role = primaryRole,
                    CallCount = userActs.Count(a => a.ActivityType == ActivityTypes.Call),
                    MeetingCount = userActs.Count(a => a.ActivityType == ActivityTypes.Meeting),
                    EmailCount = userActs.Count(a => a.ActivityType == ActivityTypes.Email),
                    TaskCount = userActs.Count(a => a.ActivityType == ActivityTypes.Task),
                    CompletedCount = userActs.Count(a => a.Status == ActivityStatuses.Completed)
                });
            }

            var detailed = activities.Take(50).Select(a => new ActivityReportItem
            {
                ActivityId = a.ActivityId,
                ActivityType = a.ActivityType,
                Subject = a.Subject,
                RelatedTo = a.Customer != null ? a.Customer.CustomerName : (a.Lead != null ? a.Lead.LeadName : "-"),
                ActivityDate = a.ActivityDate,
                Status = a.Status,
                OwnerName = a.AssignedUser != null ? (a.AssignedUser.FullName ?? a.AssignedUser.Email) : "Unassigned"
            }).ToList();

            return new UserActivityReportViewModel
            {
                Filter = filter,
                UserSummaries = userSummaries.OrderByDescending(u => u.TotalActivities).ToList(),
                DetailedActivities = detailed
            };
        }

        public async Task<AuditReportViewModel> GetAuditReportAsync(
            string userId, string role, ReportFilterViewModel filter)
        {
            var query = _context.AuditLogs
                .Include(a => a.User)
                .AsNoTracking();

            if (filter.FromDate.HasValue)
            {
                var fromUtc = filter.FromDate.Value.Date;
                query = query.Where(a => a.CreatedDate >= fromUtc);
            }

            if (filter.ToDate.HasValue)
            {
                var toUtc = filter.ToDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(a => a.CreatedDate <= toUtc);
            }

            if (!string.IsNullOrEmpty(filter.Status))
            {
                query = query.Where(a => a.Result == filter.Status);
            }

            if (!string.IsNullOrEmpty(filter.AssignedTo))
            {
                query = query.Where(a => a.UserId == filter.AssignedTo);
            }

            var logs = await query
                .OrderByDescending(a => a.CreatedDate)
                .Take(100)
                .Select(a => new AuditReportItem
                {
                    AuditLogId = a.AuditLogId,
                    UserId = a.UserId,
                    UserName = a.User != null ? (a.User.FullName ?? a.User.Email) : "System",
                    UserEmail = a.User != null ? a.User.Email : string.Empty,
                    Action = a.Action,
                    EntityName = a.EntityName,
                    RecordId = a.RecordId,
                    OldValue = a.OldValue,
                    NewValue = a.NewValue,
                    CreatedDate = a.CreatedDate,
                    IpAddress = a.IpAddress,
                    Result = a.Result,
                    Details = a.Details
                })
                .ToListAsync();

            var totalCount = await query.CountAsync();
            var securityCount = await query.CountAsync(a => a.Action.Contains("Login") || a.Action.Contains("Logout") || a.Action.Contains("Password") || a.Action.Contains("Role"));
            var businessCount = totalCount - securityCount;

            return new AuditReportViewModel
            {
                Filter = filter,
                Logs = logs,
                TotalEvents = totalCount,
                SecurityEventsCount = securityCount,
                BusinessEventsCount = businessCount
            };
        }
    }
}
