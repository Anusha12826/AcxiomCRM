using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Models.Constants;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Services
{
    public class OpportunityService : IOpportunityService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;
        private readonly UserManager<ApplicationUser> _userManager;

        public OpportunityService(
            ApplicationDbContext context,
            IAuditService auditService,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _auditService = auditService;
            _userManager = userManager;
        }

        public async Task<OpportunityListViewModel> GetOpportunitiesAsync(
            string userId,
            string role,
            string? search = null,
            string? stage = null,
            string? status = null,
            string? assignedTo = null,
            int pageIndex = 1,
            int pageSize = 10)
        {
            var baseQuery = _context.Opportunities
                .Include(o => o.Customer)
                .Include(o => o.Lead)
                .Include(o => o.AssignedUser)
                .AsQueryable();

            if (role == AppRoles.SalesExecutive)
            {
                baseQuery = baseQuery.Where(o => o.AssignedTo == userId);
            }

            // Memory projection for KPI sums (avoids SQLite decimal aggregation issue)
            var metrics = await baseQuery
                .Select(o => new { o.Amount, o.Probability, o.Status })
                .ToListAsync();

            var openCount = metrics.Count(o => o.Status == OpportunityStatuses.Open);
            var wonCount = metrics.Count(o => o.Status == OpportunityStatuses.Won);
            var lostCount = metrics.Count(o => o.Status == OpportunityStatuses.Lost);

            var totalPipeline = metrics
                .Where(o => o.Status == OpportunityStatuses.Open)
                .Sum(o => o.Amount);

            var weightedPipeline = metrics
                .Where(o => o.Status == OpportunityStatuses.Open)
                .Sum(o => o.Amount * o.Probability / 100m);

            var query = baseQuery;

            // Search filter
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(o =>
                    o.OpportunityName.Contains(term) ||
                    (o.Customer != null && o.Customer.CustomerName.Contains(term)) ||
                    (o.Lead != null && o.Lead.LeadName.Contains(term)));
            }

            // Stage filter
            if (!string.IsNullOrWhiteSpace(stage))
            {
                query = query.Where(o => o.Stage == stage);
            }

            // Status filter
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(o => o.Status == status);
            }

            // Assigned to filter
            if (!string.IsNullOrWhiteSpace(assignedTo))
            {
                query = query.Where(o => o.AssignedTo == assignedTo);
            }

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            if (totalPages < 1) totalPages = 1;
            if (pageIndex < 1) pageIndex = 1;
            if (pageIndex > totalPages) pageIndex = totalPages;

            var opportunities = await query
                .OrderByDescending(o => o.CreatedDate)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var salesExecutives = await GetSalesExecutivesAsync();

            return new OpportunityListViewModel
            {
                Opportunities = opportunities,
                SearchTerm = search,
                StageFilter = stage,
                StatusFilter = status,
                AssignedToFilter = assignedTo,
                PageIndex = pageIndex,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                OpenCount = openCount,
                WonCount = wonCount,
                LostCount = lostCount,
                TotalPipelineValue = totalPipeline,
                TotalWeightedPipelineValue = weightedPipeline,
                SalesExecutives = salesExecutives
            };
        }

        public async Task<Opportunity?> GetOpportunityByIdAsync(int id, string userId, string role)
        {
            var query = _context.Opportunities
                .Include(o => o.Customer)
                .Include(o => o.Lead)
                .Include(o => o.AssignedUser)
                .AsQueryable();

            if (role == AppRoles.SalesExecutive)
            {
                query = query.Where(o => o.AssignedTo == userId);
            }

            return await query.FirstOrDefaultAsync(o => o.OpportunityId == id);
        }

        public async Task<OpportunityDetailsViewModel?> GetOpportunityDetailsAsync(int id, string userId, string role)
        {
            var opp = await GetOpportunityByIdAsync(id, userId, role);
            if (opp == null) return null;

            var followUps = await _context.FollowUps
                .Where(f => f.OpportunityId == id || (f.CustomerId != null && f.CustomerId == opp.CustomerId))
                .OrderByDescending(f => f.FollowUpDate)
                .ToListAsync();

            var activities = await _context.Activities
                .Where(a => a.CustomerId != null && a.CustomerId == opp.CustomerId)
                .OrderByDescending(a => a.ActivityDate)
                .ToListAsync();

            var auditLogs = await _context.AuditLogs
                .Where(a => a.EntityName == "Opportunity" && a.RecordId == id.ToString())
                .OrderByDescending(a => a.CreatedDate)
                .ToListAsync();

            return new OpportunityDetailsViewModel
            {
                Opportunity = opp,
                FollowUps = followUps,
                Activities = activities,
                AuditHistory = auditLogs
            };
        }

        public async Task<bool> CreateOpportunityAsync(Opportunity opportunity, string currentUserId)
        {
            opportunity.CreatedDate = DateTime.UtcNow;

            // Align status and probability with initial stage
            if (opportunity.Stage == OpportunityStages.Won)
            {
                opportunity.Status = OpportunityStatuses.Won;
                opportunity.Probability = 100;
            }
            else if (opportunity.Stage == OpportunityStages.Lost)
            {
                opportunity.Status = OpportunityStatuses.Lost;
                opportunity.Probability = 0;
            }
            else
            {
                opportunity.Status = OpportunityStatuses.Open;
            }

            _context.Opportunities.Add(opportunity);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                userId: currentUserId,
                action: "OpportunityCreated",
                entityName: "Opportunity",
                recordId: opportunity.OpportunityId.ToString(),
                oldValue: null,
                newValue: $"Name: {opportunity.OpportunityName}, Amount: {opportunity.Amount}, Stage: {opportunity.Stage}, Probability: {opportunity.Probability}%",
                result: "Success",
                details: $"Opportunity '{opportunity.OpportunityName}' created with pipeline value ₹{opportunity.Amount:N2}.");

            return true;
        }

        public async Task<bool> UpdateOpportunityAsync(Opportunity opportunity, string currentUserId)
        {
            var existing = await _context.Opportunities.FindAsync(opportunity.OpportunityId);
            if (existing == null) return false;

            var oldValue = $"Name: {existing.OpportunityName}, Amount: {existing.Amount}, Stage: {existing.Stage}, Prob: {existing.Probability}%, Status: {existing.Status}";

            existing.OpportunityName = opportunity.OpportunityName;
            existing.CustomerId = opportunity.CustomerId;
            existing.LeadId = opportunity.LeadId;
            existing.Amount = opportunity.Amount;
            existing.Stage = opportunity.Stage;
            existing.Probability = opportunity.Probability;
            existing.ExpectedCloseDate = opportunity.ExpectedCloseDate;
            existing.Status = opportunity.Status;
            existing.AssignedTo = opportunity.AssignedTo;

            // Stage synchronization
            if (existing.Stage == OpportunityStages.Won)
            {
                existing.Status = OpportunityStatuses.Won;
                existing.Probability = 100;
            }
            else if (existing.Stage == OpportunityStages.Lost)
            {
                existing.Status = OpportunityStatuses.Lost;
                existing.Probability = 0;
            }

            await _context.SaveChangesAsync();

            var newValue = $"Name: {existing.OpportunityName}, Amount: {existing.Amount}, Stage: {existing.Stage}, Prob: {existing.Probability}%, Status: {existing.Status}";

            await _auditService.LogAsync(
                userId: currentUserId,
                action: "OpportunityUpdated",
                entityName: "Opportunity",
                recordId: existing.OpportunityId.ToString(),
                oldValue: oldValue,
                newValue: newValue,
                result: "Success",
                details: $"Opportunity '{existing.OpportunityName}' modified.");

            return true;
        }

        public async Task<bool> UpdateStageAsync(int id, string newStage, string currentUserId)
        {
            var opp = await _context.Opportunities.FindAsync(id);
            if (opp == null) return false;

            var oldStage = opp.Stage;
            var oldStatus = opp.Status;
            var oldProb = opp.Probability;

            opp.Stage = newStage;

            if (newStage == OpportunityStages.Won)
            {
                opp.Status = OpportunityStatuses.Won;
                opp.Probability = 100;
            }
            else if (newStage == OpportunityStages.Lost)
            {
                opp.Status = OpportunityStatuses.Lost;
                opp.Probability = 0;
            }
            else
            {
                opp.Status = OpportunityStatuses.Open;
            }

            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                userId: currentUserId,
                action: "OpportunityStageChanged",
                entityName: "Opportunity",
                recordId: opp.OpportunityId.ToString(),
                oldValue: $"Stage: {oldStage}, Status: {oldStatus}, Probability: {oldProb}%",
                newValue: $"Stage: {opp.Stage}, Status: {opp.Status}, Probability: {opp.Probability}%",
                result: "Success",
                details: $"Opportunity '{opp.OpportunityName}' stage transitioned from '{oldStage}' to '{newStage}'.");

            return true;
        }

        public async Task<bool> DeleteOpportunityAsync(int id, string currentUserId)
        {
            var opp = await _context.Opportunities.FindAsync(id);
            if (opp == null) return false;

            _context.Opportunities.Remove(opp);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                userId: currentUserId,
                action: "OpportunityDeleted",
                entityName: "Opportunity",
                recordId: id.ToString(),
                oldValue: $"Name: {opp.OpportunityName}, Amount: {opp.Amount}",
                newValue: null,
                result: "Success",
                details: $"Opportunity '{opp.OpportunityName}' deleted.");

            return true;
        }

        public async Task<Dictionary<int, string>> GetCustomersDropdownAsync(string userId, string role)
        {
            var query = _context.Customers.Where(c => c.Status == CustomerStatuses.Active).AsQueryable();
            if (role == AppRoles.SalesExecutive)
            {
                query = query.Where(c => c.AssignedTo == userId);
            }

            return await query
                .OrderBy(c => c.CustomerName)
                .ToDictionaryAsync(c => c.CustomerId, c => $"{c.CustomerName} ({c.CustomerCode})");
        }

        public async Task<Dictionary<int, string>> GetLeadsDropdownAsync(string userId, string role)
        {
            var query = _context.Leads.AsQueryable();
            if (role == AppRoles.SalesExecutive)
            {
                query = query.Where(l => l.AssignedTo == userId);
            }

            return await query
                .OrderBy(l => l.LeadName)
                .ToDictionaryAsync(l => l.LeadId, l => $"{l.LeadName} ({l.LeadCode})");
        }

        public async Task<Dictionary<string, string>> GetSalesExecutivesAsync()
        {
            var users = await _userManager.Users.Where(u => u.IsActive).ToListAsync();
            var dict = new Dictionary<string, string>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Contains(AppRoles.SalesExecutive) || roles.Contains(AppRoles.Manager) || roles.Contains(AppRoles.Admin))
                {
                    dict[user.Id] = string.IsNullOrEmpty(user.FullName) ? user.Email! : $"{user.FullName} ({string.Join(",", roles)})";
                }
            }

            return dict;
        }
    }
}
