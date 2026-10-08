using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Models.Constants;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;
        private readonly UserManager<ApplicationUser> _userManager;

        public CustomerService(
            ApplicationDbContext context,
            IAuditService auditService,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _auditService = auditService;
            _userManager = userManager;
        }

        public async Task<CustomerListViewModel> GetCustomersAsync(
            string userId,
            string role,
            string? searchTerm = null,
            string? statusFilter = null,
            string? assignedToFilter = null,
            int pageIndex = 1,
            int pageSize = 10)
        {
            var query = _context.Customers
                .Include(c => c.Creator)
                .Include(c => c.AssignedUser)
                .AsQueryable();

            // Role-based scope
            if (role == AppRoles.SalesExecutive)
            {
                query = query.Where(c => c.AssignedTo == userId || c.CreatedBy == userId);
            }

            // Search filter: Name, Email, Phone, Company
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim();
                query = query.Where(c =>
                    c.CustomerName.Contains(term) ||
                    c.Email.Contains(term) ||
                    c.Phone.Contains(term) ||
                    (c.CompanyName != null && c.CompanyName.Contains(term)) ||
                    c.CustomerCode.Contains(term));
            }

            // Status filter
            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                query = query.Where(c => c.Status == statusFilter);
            }

            // Assigned to filter
            if (!string.IsNullOrWhiteSpace(assignedToFilter))
            {
                query = query.Where(c => c.AssignedTo == assignedToFilter);
            }

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            if (totalPages < 1) totalPages = 1;
            if (pageIndex < 1) pageIndex = 1;
            if (pageIndex > totalPages) pageIndex = totalPages;

            var customers = await query
                .OrderByDescending(c => c.CreatedDate)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var salesExecutives = await GetSalesExecutivesAsync();

            return new CustomerListViewModel
            {
                Customers = customers,
                SearchTerm = searchTerm,
                StatusFilter = statusFilter,
                AssignedToFilter = assignedToFilter,
                PageIndex = pageIndex,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                SalesExecutives = salesExecutives
            };
        }

        public async Task<Customer?> GetCustomerByIdAsync(int id, string userId, string role)
        {
            var query = _context.Customers
                .Include(c => c.Creator)
                .Include(c => c.AssignedUser)
                .AsQueryable();

            if (role == AppRoles.SalesExecutive)
            {
                query = query.Where(c => c.AssignedTo == userId || c.CreatedBy == userId);
            }

            return await query.FirstOrDefaultAsync(c => c.CustomerId == id);
        }

        public async Task<CustomerDetailsViewModel?> GetCustomerDetailsAsync(int id, string userId, string role)
        {
            var customer = await GetCustomerByIdAsync(id, userId, role);
            if (customer == null) return null;

            var opportunities = await _context.Opportunities
                .Where(o => o.CustomerId == id)
                .OrderByDescending(o => o.CreatedDate)
                .ToListAsync();

            var followUps = await _context.FollowUps
                .Where(f => f.CustomerId == id)
                .OrderByDescending(f => f.FollowUpDate)
                .ToListAsync();

            var activities = await _context.Activities
                .Where(a => a.CustomerId == id)
                .OrderByDescending(a => a.ActivityDate)
                .ToListAsync();

            var auditLogs = await _context.AuditLogs
                .Where(a => a.EntityName == "Customer" && a.RecordId == id.ToString())
                .OrderByDescending(a => a.CreatedDate)
                .ToListAsync();

            return new CustomerDetailsViewModel
            {
                Customer = customer,
                Opportunities = opportunities,
                FollowUps = followUps,
                Activities = activities,
                AuditHistory = auditLogs
            };
        }

        public async Task<bool> CreateCustomerAsync(Customer customer, string currentUserId)
        {
            if (string.IsNullOrWhiteSpace(customer.CustomerCode))
            {
                customer.CustomerCode = await GenerateCustomerCodeAsync();
            }

            customer.CreatedDate = DateTime.UtcNow;
            customer.CreatedBy = currentUserId;

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                userId: currentUserId,
                action: "CustomerCreated",
                entityName: "Customer",
                recordId: customer.CustomerId.ToString(),
                oldValue: null,
                newValue: $"Code: {customer.CustomerCode}, Name: {customer.CustomerName}, Email: {customer.Email}, Phone: {customer.Phone}",
                result: "Success",
                details: $"Customer '{customer.CustomerName}' created.");

            return true;
        }

        public async Task<bool> UpdateCustomerAsync(Customer customer, string currentUserId)
        {
            var existing = await _context.Customers.FindAsync(customer.CustomerId);
            if (existing == null) return false;

            var oldValues = $"Name: {existing.CustomerName}, Email: {existing.Email}, Phone: {existing.Phone}, Status: {existing.Status}, AssignedTo: {existing.AssignedTo}";

            existing.CustomerName = customer.CustomerName;
            existing.Email = customer.Email;
            existing.Phone = customer.Phone;
            existing.CompanyName = customer.CompanyName;
            existing.Address = customer.Address;
            existing.City = customer.City;
            existing.State = customer.State;
            existing.Status = customer.Status;
            existing.AssignedTo = customer.AssignedTo;

            await _context.SaveChangesAsync();

            var newValues = $"Name: {existing.CustomerName}, Email: {existing.Email}, Phone: {existing.Phone}, Status: {existing.Status}, AssignedTo: {existing.AssignedTo}";

            await _auditService.LogAsync(
                userId: currentUserId,
                action: "CustomerUpdated",
                entityName: "Customer",
                recordId: existing.CustomerId.ToString(),
                oldValue: oldValues,
                newValue: newValues,
                result: "Success",
                details: $"Customer '{existing.CustomerName}' updated.");

            return true;
        }

        public async Task<bool> DeactivateCustomerAsync(int id, string currentUserId)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null) return false;

            var oldStatus = customer.Status;
            customer.Status = CustomerStatuses.Inactive;
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                userId: currentUserId,
                action: "CustomerDeactivated",
                entityName: "Customer",
                recordId: customer.CustomerId.ToString(),
                oldValue: $"Status: {oldStatus}",
                newValue: $"Status: {customer.Status}",
                result: "Success",
                details: $"Customer '{customer.CustomerName}' deactivated.");

            return true;
        }

        public async Task<bool> DeleteCustomerAsync(int id, string currentUserId)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null) return false;

            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                userId: currentUserId,
                action: "CustomerDeleted",
                entityName: "Customer",
                recordId: id.ToString(),
                oldValue: $"Name: {customer.CustomerName}, Code: {customer.CustomerCode}",
                newValue: null,
                result: "Success",
                details: $"Customer '{customer.CustomerName}' deleted.");

            return true;
        }

        public async Task<bool> IsEmailUniqueAsync(string email, int? excludeCustomerId = null)
        {
            if (string.IsNullOrWhiteSpace(email)) return true;
            var normalized = email.Trim().ToLower();

            var query = _context.Customers.Where(c => c.Email.ToLower() == normalized);
            if (excludeCustomerId.HasValue)
            {
                query = query.Where(c => c.CustomerId != excludeCustomerId.Value);
            }

            return !await query.AnyAsync();
        }

        public async Task<bool> IsPhoneUniqueAsync(string phone, int? excludeCustomerId = null)
        {
            if (string.IsNullOrWhiteSpace(phone)) return true;
            var cleanPhone = phone.Trim();

            var query = _context.Customers.Where(c => c.Phone == cleanPhone);
            if (excludeCustomerId.HasValue)
            {
                query = query.Where(c => c.CustomerId != excludeCustomerId.Value);
            }

            return !await query.AnyAsync();
        }

        public async Task<string> GenerateCustomerCodeAsync()
        {
            var count = await _context.Customers.CountAsync();
            return $"CUST-{(1000 + count + 1)}";
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
