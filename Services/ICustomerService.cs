using AcxiomCRM.Models;
using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Services
{
    public interface ICustomerService
    {
        Task<CustomerListViewModel> GetCustomersAsync(
            string userId,
            string role,
            string? searchTerm = null,
            string? statusFilter = null,
            string? assignedToFilter = null,
            int pageIndex = 1,
            int pageSize = 10);

        Task<Customer?> GetCustomerByIdAsync(int id, string userId, string role);

        Task<CustomerDetailsViewModel?> GetCustomerDetailsAsync(int id, string userId, string role);

        Task<bool> CreateCustomerAsync(Customer customer, string currentUserId);

        Task<bool> UpdateCustomerAsync(Customer customer, string currentUserId);

        Task<bool> DeactivateCustomerAsync(int id, string currentUserId);

        Task<bool> DeleteCustomerAsync(int id, string currentUserId);

        Task<bool> IsEmailUniqueAsync(string email, int? excludeCustomerId = null);

        Task<bool> IsPhoneUniqueAsync(string phone, int? excludeCustomerId = null);

        Task<string> GenerateCustomerCodeAsync();

        Task<Dictionary<string, string>> GetSalesExecutivesAsync();
    }
}
