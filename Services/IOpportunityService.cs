using AcxiomCRM.Models;
using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Services
{
    public interface IOpportunityService
    {
        Task<OpportunityListViewModel> GetOpportunitiesAsync(
            string userId,
            string role,
            string? search = null,
            string? stage = null,
            string? status = null,
            string? assignedTo = null,
            int pageIndex = 1,
            int pageSize = 10);

        Task<Opportunity?> GetOpportunityByIdAsync(int id, string userId, string role);

        Task<OpportunityDetailsViewModel?> GetOpportunityDetailsAsync(int id, string userId, string role);

        Task<bool> CreateOpportunityAsync(Opportunity opportunity, string currentUserId);

        Task<bool> UpdateOpportunityAsync(Opportunity opportunity, string currentUserId);

        Task<bool> UpdateStageAsync(int id, string newStage, string currentUserId);

        Task<bool> DeleteOpportunityAsync(int id, string currentUserId);

        Task<Dictionary<int, string>> GetCustomersDropdownAsync(string userId, string role);

        Task<Dictionary<int, string>> GetLeadsDropdownAsync(string userId, string role);

        Task<Dictionary<string, string>> GetSalesExecutivesAsync();
    }
}
