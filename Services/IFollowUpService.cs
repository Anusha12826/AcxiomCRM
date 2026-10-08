using AcxiomCRM.Models;
using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Services
{
    public interface IFollowUpService
    {
        Task<FollowUpListViewModel> GetFollowUpsAsync(
            string userId,
            string role,
            string? filterTab = "all",
            string? typeFilter = null,
            string? assignedTo = null,
            int pageIndex = 1,
            int pageSize = 10);

        Task<FollowUp?> GetFollowUpByIdAsync(int id, string userId, string role);

        Task<bool> CreateFollowUpAsync(FollowUp followUp, string currentUserId);

        Task<bool> UpdateFollowUpAsync(FollowUp followUp, string currentUserId);

        Task<bool> CompleteFollowUpAsync(int id, string completionRemarks, string currentUserId);

        Task<bool> RescheduleFollowUpAsync(int id, DateTime newDate, string? reason, string currentUserId);

        Task<bool> CancelFollowUpAsync(int id, string? reason, string currentUserId);

        Task<bool> DeleteFollowUpAsync(int id, string currentUserId);

        Task<Dictionary<int, string>> GetCustomersDropdownAsync(string userId, string role);

        Task<Dictionary<int, string>> GetLeadsDropdownAsync(string userId, string role);

        Task<Dictionary<string, string>> GetSalesExecutivesAsync();
    }
}
