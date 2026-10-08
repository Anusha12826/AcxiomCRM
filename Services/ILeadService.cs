using AcxiomCRM.Models;
using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Services
{
    public interface ILeadService
    {
        Task<LeadListViewModel> GetLeadsAsync(
            string userId,
            string role,
            string? search = null,
            string? status = null,
            string? priority = null,
            string? assignedTo = null,
            int pageIndex = 1,
            int pageSize = 10);

        Task<Lead?> GetLeadByIdAsync(int id, string userId, string role);

        Task<LeadDetailsViewModel?> GetLeadDetailsAsync(int id, string userId, string role);

        Task<bool> CreateLeadAsync(Lead lead, string currentUserId);

        Task<bool> UpdateLeadAsync(Lead lead, string currentUserId);

        Task<bool> UpdateStatusAsync(int leadId, string newStatus, string currentUserId);

        Task<bool> DeleteLeadAsync(int leadId, string currentUserId);

        Task<string> GenerateLeadCodeAsync();

        Task<(bool Success, string Message, int? CustomerId, int? OpportunityId)> ConvertLeadAsync(
            LeadConvertViewModel model,
            string currentUserId);

        Task<Dictionary<string, string>> GetSalesExecutivesAsync();
    }
}
