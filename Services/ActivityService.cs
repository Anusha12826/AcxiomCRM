using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Models.Constants;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Services
{
    public class ActivityService : IActivityService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ActivityService(
            ApplicationDbContext context,
            IAuditService auditService,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _auditService = auditService;
            _userManager = userManager;
        }

        public async Task<ActivityListViewModel> GetActivitiesAsync(
            string userId,
            string role,
            string? search = null,
            string? type = null,
            string? status = null,
            string? assignedTo = null,
            int pageIndex = 1,
            int pageSize = 10)
        {
            var baseQuery = _context.Activities
                .Include(a => a.Customer)
                .Include(a => a.Lead)
                .Include(a => a.AssignedUser)
                .AsQueryable();

            if (role == AppRoles.SalesExecutive)
            {
                baseQuery = baseQuery.Where(a => a.AssignedTo == userId);
            }

            var callCount = await baseQuery.CountAsync(a => a.ActivityType == ActivityTypes.Call);
            var meetingCount = await baseQuery.CountAsync(a => a.ActivityType == ActivityTypes.Meeting);
            var emailCount = await baseQuery.CountAsync(a => a.ActivityType == ActivityTypes.Email);
            var taskCount = await baseQuery.CountAsync(a => a.ActivityType == ActivityTypes.Task);

            var query = baseQuery;

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(a =>
                    a.Subject.Contains(term) ||
                    (a.Description != null && a.Description.Contains(term)) ||
                    (a.Customer != null && a.Customer.CustomerName.Contains(term)) ||
                    (a.Lead != null && a.Lead.LeadName.Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(type))
            {
                query = query.Where(a => a.ActivityType == type);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(a => a.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(assignedTo))
            {
                query = query.Where(a => a.AssignedTo == assignedTo);
            }

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            if (totalPages < 1) totalPages = 1;
            if (pageIndex < 1) pageIndex = 1;
            if (pageIndex > totalPages) pageIndex = totalPages;

            var activities = await query
                .OrderByDescending(a => a.ActivityDate)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var salesExecutives = await GetSalesExecutivesAsync();

            return new ActivityListViewModel
            {
                Activities = activities,
                SearchTerm = search,
                TypeFilter = type,
                StatusFilter = status,
                AssignedToFilter = assignedTo,
                PageIndex = pageIndex,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                CallCount = callCount,
                MeetingCount = meetingCount,
                EmailCount = emailCount,
                TaskCount = taskCount,
                SalesExecutives = salesExecutives
            };
        }

        public async Task<Activity?> GetActivityByIdAsync(int id, string userId, string role)
        {
            var query = _context.Activities
                .Include(a => a.Customer)
                .Include(a => a.Lead)
                .Include(a => a.AssignedUser)
                .AsQueryable();

            if (role == AppRoles.SalesExecutive)
            {
                query = query.Where(a => a.AssignedTo == userId);
            }

            return await query.FirstOrDefaultAsync(a => a.ActivityId == id);
        }

        public async Task<bool> CreateActivityAsync(Activity activity, string currentUserId)
        {
            activity.CreatedDate = DateTime.UtcNow;

            _context.Activities.Add(activity);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                userId: currentUserId,
                action: "ActivityCreated",
                entityName: "Activity",
                recordId: activity.ActivityId.ToString(),
                oldValue: null,
                newValue: $"Type: {activity.ActivityType}, Subject: {activity.Subject}, Status: {activity.Status}",
                result: "Success",
                details: $"Activity '{activity.Subject}' ({activity.ActivityType}) logged.");

            return true;
        }

        public async Task<bool> UpdateActivityAsync(Activity activity, string currentUserId)
        {
            var existing = await _context.Activities.FindAsync(activity.ActivityId);
            if (existing == null) return false;

            var oldValue = $"Type: {existing.ActivityType}, Subject: {existing.Subject}, Status: {existing.Status}";

            existing.ActivityType = activity.ActivityType;
            existing.Subject = activity.Subject;
            existing.Description = activity.Description;
            existing.ActivityDate = activity.ActivityDate;
            existing.CustomerId = activity.CustomerId;
            existing.LeadId = activity.LeadId;
            existing.Status = activity.Status;
            existing.AssignedTo = activity.AssignedTo;

            await _context.SaveChangesAsync();

            var newValue = $"Type: {existing.ActivityType}, Subject: {existing.Subject}, Status: {existing.Status}";

            await _auditService.LogAsync(
                userId: currentUserId,
                action: "ActivityUpdated",
                entityName: "Activity",
                recordId: existing.ActivityId.ToString(),
                oldValue: oldValue,
                newValue: newValue,
                result: "Success",
                details: $"Activity #{existing.ActivityId} updated.");

            return true;
        }

        public async Task<bool> UpdateStatusAsync(int id, string newStatus, string currentUserId)
        {
            var existing = await _context.Activities.FindAsync(id);
            if (existing == null) return false;

            var oldStatus = existing.Status;
            existing.Status = newStatus;
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                userId: currentUserId,
                action: "ActivityStatusChanged",
                entityName: "Activity",
                recordId: existing.ActivityId.ToString(),
                oldValue: $"Status: {oldStatus}",
                newValue: $"Status: {newStatus}",
                result: "Success",
                details: $"Activity '{existing.Subject}' status updated from '{oldStatus}' to '{newStatus}'.");

            return true;
        }

        public async Task<bool> DeleteActivityAsync(int id, string currentUserId)
        {
            var existing = await _context.Activities.FindAsync(id);
            if (existing == null) return false;

            _context.Activities.Remove(existing);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                userId: currentUserId,
                action: "ActivityDeleted",
                entityName: "Activity",
                recordId: id.ToString(),
                oldValue: $"Subject: {existing.Subject}, Type: {existing.ActivityType}",
                newValue: null,
                result: "Success",
                details: $"Activity '{existing.Subject}' deleted.");

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
