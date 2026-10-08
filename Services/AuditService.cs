using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Services
{
    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuditService> _logger;

        public AuditService(ApplicationDbContext context, ILogger<AuditService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task LogAsync(
            string? userId,
            string action,
            string? entityName,
            string? recordId,
            string? oldValue,
            string? newValue,
            string? result = "Success",
            string? details = null,
            string? ipAddress = null)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    UserId = userId,
                    Action = action,
                    EntityName = entityName,
                    RecordId = recordId,
                    OldValue = oldValue,
                    NewValue = newValue,
                    Result = result,
                    Details = details,
                    IpAddress = ipAddress,
                    CreatedDate = DateTime.UtcNow
                };

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to persist audit log entry for action {Action} on {EntityName}", action, entityName);
            }
        }

        public async Task<IEnumerable<AuditLog>> GetAuditLogsAsync(
            string? userId = null,
            string? entityName = null,
            string? action = null,
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            var query = _context.AuditLogs.Include(a => a.User).AsQueryable();

            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(a => a.UserId == userId);
            }

            if (!string.IsNullOrEmpty(entityName))
            {
                query = query.Where(a => a.EntityName == entityName);
            }

            if (!string.IsNullOrEmpty(action))
            {
                query = query.Where(a => a.Action == action);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(a => a.CreatedDate >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(a => a.CreatedDate <= toDate.Value);
            }

            return await query.OrderByDescending(a => a.CreatedDate).ToListAsync();
        }

        public async Task<AuditLogListViewModel> GetPagedAuditLogsAsync(
            string? userId = null,
            string? entityName = null,
            string? action = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int pageIndex = 1,
            int pageSize = 15)
        {
            var query = _context.AuditLogs
                .Include(a => a.User)
                .AsQueryable();

            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(a => a.UserId == userId);
            }

            if (!string.IsNullOrEmpty(entityName))
            {
                query = query.Where(a => a.EntityName == entityName);
            }

            if (!string.IsNullOrEmpty(action))
            {
                query = query.Where(a => a.Action == action);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(a => a.CreatedDate >= fromDate.Value.Date);
            }

            if (toDate.HasValue)
            {
                var endOfDay = toDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(a => a.CreatedDate <= endOfDay);
            }

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            if (totalPages < 1) totalPages = 1;
            if (pageIndex < 1) pageIndex = 1;
            if (pageIndex > totalPages) pageIndex = totalPages;

            var logs = await query
                .OrderByDescending(a => a.CreatedDate)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Available filters
            var users = await _context.Users
                .ToDictionaryAsync(u => u.Id, u => string.IsNullOrEmpty(u.FullName) ? u.Email! : $"{u.FullName} ({u.Email})");

            var entities = await _context.AuditLogs
                .Where(a => a.EntityName != null)
                .Select(a => a.EntityName!)
                .Distinct()
                .OrderBy(e => e)
                .ToListAsync();

            var actions = await _context.AuditLogs
                .Select(a => a.Action)
                .Distinct()
                .OrderBy(a => a)
                .ToListAsync();

            return new AuditLogListViewModel
            {
                AuditLogs = logs,
                UserIdFilter = userId,
                EntityNameFilter = entityName,
                ActionFilter = action,
                FromDate = fromDate,
                ToDate = toDate,
                PageIndex = pageIndex,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                AvailableUsers = users,
                AvailableEntities = entities,
                AvailableActions = actions
            };
        }

        public async Task<AuditLog?> GetAuditLogByIdAsync(long id)
        {
            return await _context.AuditLogs
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.AuditLogId == id);
        }
    }
}
