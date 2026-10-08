using AcxiomCRM.Models;

namespace AcxiomCRM.ViewModels
{
    public class AuditLogListViewModel
    {
        public IEnumerable<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
        public string? UserIdFilter { get; set; }
        public string? EntityNameFilter { get; set; }
        public string? ActionFilter { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 15;
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }

        public Dictionary<string, string> AvailableUsers { get; set; } = new();
        public List<string> AvailableEntities { get; set; } = new();
        public List<string> AvailableActions { get; set; } = new();

        public bool HasPreviousPage => PageIndex > 1;
        public bool HasNextPage => PageIndex < TotalPages;
    }

    public class AuditLogDetailsViewModel
    {
        public AuditLog AuditLog { get; set; } = null!;
    }
}
