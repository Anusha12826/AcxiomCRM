using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Models.Constants;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Services
{
    public class FollowUpService : IFollowUpService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;
        private readonly UserManager<ApplicationUser> _userManager;

        public FollowUpService(
            ApplicationDbContext context,
            IAuditService auditService,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _auditService = auditService;
            _userManager = userManager;
        }

        public async Task<FollowUpListViewModel> GetFollowUpsAsync(
            string userId,
            string role,
            string? filterTab = "all",
            string? typeFilter = null,
            string? assignedTo = null,
            int pageIndex = 1,
            int pageSize = 10)
        {
            var baseQuery = _context.FollowUps
                .Include(f => f.Customer)
                .Include(f => f.Lead)
                .Include(f => f.Opportunity)
                .Include(f => f.AssignedUser)
                .AsQueryable();

            if (role == AppRoles.SalesExecutive)
            {
                baseQuery = baseQuery.Where(f => f.AssignedTo == userId);
            }

            var now = DateTime.UtcNow;

            // Calculate Counts for KPIs
            var pendingCount = await baseQuery
                .CountAsync(f => f.Status == FollowUpStatuses.Planned && f.FollowUpDate >= now);

            var overdueCount = await baseQuery
                .CountAsync(f => f.Status == FollowUpStatuses.Planned && f.FollowUpDate < now);

            var completedCount = await baseQuery
                .CountAsync(f => f.Status == FollowUpStatuses.Completed);

            var query = baseQuery;

            // Apply Tab filter
            switch (filterTab?.ToLower())
            {
                case "pending":
                    query = query.Where(f => f.Status == FollowUpStatuses.Planned && f.FollowUpDate >= now);
                    break;
                case "overdue":
                    query = query.Where(f => f.Status == FollowUpStatuses.Planned && f.FollowUpDate < now);
                    break;
                case "completed":
                    query = query.Where(f => f.Status == FollowUpStatuses.Completed);
                    break;
                case "cancelled":
                    query = query.Where(f => f.Status == FollowUpStatuses.Cancelled);
                    break;
                default:
                    filterTab = "all";
                    break;
            }

            // Apply Type filter
            if (!string.IsNullOrWhiteSpace(typeFilter))
            {
                query = query.Where(f => f.FollowUpType == typeFilter);
            }

            // Apply Assigned To filter
            if (!string.IsNullOrWhiteSpace(assignedTo))
            {
                query = query.Where(f => f.AssignedTo == assignedTo);
            }

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            if (totalPages < 1) totalPages = 1;
            if (pageIndex < 1) pageIndex = 1;
            if (pageIndex > totalPages) pageIndex = totalPages;

            // Order: overdue first, then chronological
            var followUps = await query
                .OrderBy(f => f.FollowUpDate)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var salesExecutives = await GetSalesExecutivesAsync();

            return new FollowUpListViewModel
            {
                FollowUps = followUps,
                FilterTab = filterTab,
                TypeFilter = typeFilter,
                AssignedToFilter = assignedTo,
                PageIndex = pageIndex,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                PendingCount = pendingCount,
                OverdueCount = overdueCount,
                CompletedCount = completedCount,
                SalesExecutives = salesExecutives
            };
        }

        public async Task<FollowUp?> GetFollowUpByIdAsync(int id, string userId, string role)
        {
            var query = _context.FollowUps
                .Include(f => f.Customer)
                .Include(f => f.Lead)
                .Include(f => f.Opportunity)
                .Include(f => f.AssignedUser)
                .AsQueryable();

            if (role == AppRoles.SalesExecutive)
            {
                query = query.Where(f => f.AssignedTo == userId);
            }

            return await query.FirstOrDefaultAsync(f => f.FollowUpId == id);
        }

        public async Task<bool> CreateFollowUpAsync(FollowUp followUp, string currentUserId)
        {
            followUp.CreatedDate = DateTime.UtcNow;
            followUp.Status = FollowUpStatuses.Planned;

            _context.FollowUps.Add(followUp);
            await _context.SaveChangesAsync();

            var targetName = followUp.Customer?.CustomerName ?? followUp.Lead?.LeadName ?? $"Target #{followUp.CustomerId ?? followUp.LeadId}";

            await _auditService.LogAsync(
                userId: currentUserId,
                action: "FollowUpCreated",
                entityName: "FollowUp",
                recordId: followUp.FollowUpId.ToString(),
                oldValue: null,
                newValue: $"Type: {followUp.FollowUpType}, Date: {followUp.FollowUpDate:yyyy-MM-dd HH:mm}, Target: {targetName}",
                result: "Success",
                details: $"Follow-up scheduled for {followUp.FollowUpDate:yyyy-MM-dd HH:mm} via {followUp.FollowUpType}.");

            return true;
        }

        public async Task<bool> UpdateFollowUpAsync(FollowUp followUp, string currentUserId)
        {
            var existing = await _context.FollowUps.FindAsync(followUp.FollowUpId);
            if (existing == null) return false;

            var oldValue = $"Type: {existing.FollowUpType}, Date: {existing.FollowUpDate:yyyy-MM-dd HH:mm}, Status: {existing.Status}";

            existing.CustomerId = followUp.CustomerId;
            existing.LeadId = followUp.LeadId;
            existing.OpportunityId = followUp.OpportunityId;
            existing.FollowUpDate = followUp.FollowUpDate;
            existing.FollowUpType = followUp.FollowUpType;
            existing.Status = followUp.Status;
            existing.Remarks = followUp.Remarks;
            existing.AssignedTo = followUp.AssignedTo;

            await _context.SaveChangesAsync();

            var newValue = $"Type: {existing.FollowUpType}, Date: {existing.FollowUpDate:yyyy-MM-dd HH:mm}, Status: {existing.Status}";

            await _auditService.LogAsync(
                userId: currentUserId,
                action: "FollowUpUpdated",
                entityName: "FollowUp",
                recordId: existing.FollowUpId.ToString(),
                oldValue: oldValue,
                newValue: newValue,
                result: "Success",
                details: $"Follow-up #{existing.FollowUpId} modified.");

            return true;
        }

        public async Task<bool> CompleteFollowUpAsync(int id, string completionRemarks, string currentUserId)
        {
            var followUp = await _context.FollowUps
                .Include(f => f.Customer)
                .Include(f => f.Lead)
                .FirstOrDefaultAsync(f => f.FollowUpId == id);

            if (followUp == null) return false;

            var oldStatus = followUp.Status;
            followUp.Status = FollowUpStatuses.Completed;

            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm");
            followUp.Remarks = string.IsNullOrWhiteSpace(followUp.Remarks)
                ? $"[Completed {timestamp}]: {completionRemarks}"
                : $"{followUp.Remarks}\n[Completed {timestamp}]: {completionRemarks}";

            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                userId: currentUserId,
                action: "FollowUpCompleted",
                entityName: "FollowUp",
                recordId: followUp.FollowUpId.ToString(),
                oldValue: $"Status: {oldStatus}",
                newValue: $"Status: Completed, Remarks: {completionRemarks}",
                result: "Success",
                details: $"Follow-up completed. Outcome: {completionRemarks}");

            return true;
        }

        public async Task<bool> RescheduleFollowUpAsync(int id, DateTime newDate, string? reason, string currentUserId)
        {
            var followUp = await _context.FollowUps
                .Include(f => f.Customer)
                .Include(f => f.Lead)
                .FirstOrDefaultAsync(f => f.FollowUpId == id);

            if (followUp == null) return false;

            var oldDate = followUp.FollowUpDate;
            followUp.FollowUpDate = newDate;
            followUp.Status = FollowUpStatuses.Planned;

            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm");
            var note = string.IsNullOrWhiteSpace(reason) ? "No reason specified" : reason;
            followUp.Remarks = string.IsNullOrWhiteSpace(followUp.Remarks)
                ? $"[Rescheduled {timestamp} to {newDate:yyyy-MM-dd HH:mm}]: {note}"
                : $"{followUp.Remarks}\n[Rescheduled {timestamp} to {newDate:yyyy-MM-dd HH:mm}]: {note}";

            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                userId: currentUserId,
                action: "FollowUpRescheduled",
                entityName: "FollowUp",
                recordId: followUp.FollowUpId.ToString(),
                oldValue: $"Date: {oldDate:yyyy-MM-dd HH:mm}",
                newValue: $"Date: {newDate:yyyy-MM-dd HH:mm}, Reason: {note}",
                result: "Success",
                details: $"Follow-up rescheduled from {oldDate:yyyy-MM-dd HH:mm} to {newDate:yyyy-MM-dd HH:mm}.");

            return true;
        }

        public async Task<bool> CancelFollowUpAsync(int id, string? reason, string currentUserId)
        {
            var followUp = await _context.FollowUps.FindAsync(id);
            if (followUp == null) return false;

            var oldStatus = followUp.Status;
            followUp.Status = FollowUpStatuses.Cancelled;

            if (!string.IsNullOrWhiteSpace(reason))
            {
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm");
                followUp.Remarks = string.IsNullOrWhiteSpace(followUp.Remarks)
                    ? $"[Cancelled {timestamp}]: {reason}"
                    : $"{followUp.Remarks}\n[Cancelled {timestamp}]: {reason}";
            }

            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                userId: currentUserId,
                action: "FollowUpCancelled",
                entityName: "FollowUp",
                recordId: followUp.FollowUpId.ToString(),
                oldValue: $"Status: {oldStatus}",
                newValue: $"Status: Cancelled, Reason: {reason}",
                result: "Success",
                details: $"Follow-up cancelled. Reason: {reason ?? "None"}");

            return true;
        }

        public async Task<bool> DeleteFollowUpAsync(int id, string currentUserId)
        {
            var followUp = await _context.FollowUps.FindAsync(id);
            if (followUp == null) return false;

            _context.FollowUps.Remove(followUp);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                userId: currentUserId,
                action: "FollowUpDeleted",
                entityName: "FollowUp",
                recordId: id.ToString(),
                oldValue: $"Type: {followUp.FollowUpType}, Date: {followUp.FollowUpDate:yyyy-MM-dd}",
                newValue: null,
                result: "Success",
                details: $"Follow-up record deleted.");

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
            var query = _context.Leads.Where(l => l.Status != LeadStatuses.Converted).AsQueryable();
            if (role == AppRoles.SalesExecutive)
            {
                query = query.Where(l => l.AssignedTo == userId);
            }

            return await query
                .OrderBy(l => l.LeadName)
                .ToDictionaryAsync(l => l.LeadId, l => $"{l.LeadName} ({l.LeadCode} - {l.Status})");
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
