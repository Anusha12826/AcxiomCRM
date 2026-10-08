using AcxiomCRM.Models;
using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Services
{
    public interface IAuditService
    {
        Task LogAsync(
            string? userId,
            string action,
            string? entityName,
            string? recordId,
            string? oldValue,
            string? newValue,
            string? result = "Success",
            string? details = null,
            string? ipAddress = null);

        Task<IEnumerable<AuditLog>> GetAuditLogsAsync(
            string? userId = null,
            string? entityName = null,
            string? action = null,
            DateTime? fromDate = null,
            DateTime? toDate = null);

        Task<AuditLogListViewModel> GetPagedAuditLogsAsync(
            string? userId = null,
            string? entityName = null,
            string? action = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int pageIndex = 1,
            int pageSize = 15);

        Task<AuditLog?> GetAuditLogByIdAsync(long id);
    }
}
