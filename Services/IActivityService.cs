using AcxiomCRM.Models;
using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Services
{
    public interface IActivityService
    {
        Task<ActivityListViewModel> GetActivitiesAsync(
            string userId,
            string role,
            string? search = null,
            string? type = null,
            string? status = null,
            string? assignedTo = null,
            int pageIndex = 1,
            int pageSize = 10);

        Task<Activity?> GetActivityByIdAsync(int id, string userId, string role);

        Task<bool> CreateActivityAsync(Activity activity, string currentUserId);

        Task<bool> UpdateActivityAsync(Activity activity, string currentUserId);

        Task<bool> UpdateStatusAsync(int id, string newStatus, string currentUserId);

        Task<bool> DeleteActivityAsync(int id, string currentUserId);

        Task<Dictionary<int, string>> GetCustomersDropdownAsync(string userId, string role);

        Task<Dictionary<int, string>> GetLeadsDropdownAsync(string userId, string role);

        Task<Dictionary<string, string>> GetSalesExecutivesAsync();
    }
}
