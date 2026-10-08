using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Models.Constants;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Services
{
    public class LeadService : ILeadService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;
        private readonly UserManager<ApplicationUser> _userManager;

        public LeadService(
            ApplicationDbContext context,
            IAuditService auditService,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _auditService = auditService;
            _userManager = userManager;
        }

        public async Task<LeadListViewModel> GetLeadsAsync(
            string userId,
            string role,
            string? search = null,
            string? status = null,
            string? priority = null,
            string? assignedTo = null,
            int pageIndex = 1,
            int pageSize = 10)
        {
            var query = _context.Leads
                .Include(l => l.AssignedUser)
                .Include(l => l.ConvertedCustomer)
                .AsQueryable();

            // Role-based scope
            if (role == AppRoles.SalesExecutive)
            {
                query = query.Where(l => l.AssignedTo == userId);
            }

            // Search filter
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(l =>
                    l.LeadName.Contains(term) ||
                    l.Email.Contains(term) ||
                    l.Phone.Contains(term) ||
                    (l.CompanyName != null && l.CompanyName.Contains(term)) ||
                    l.LeadCode.Contains(term));
            }

            // Status filter
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(l => l.Status == status);
            }

            // Priority filter
            if (!string.IsNullOrWhiteSpace(priority))
            {
                query = query.Where(l => l.Priority == priority);
            }

            // Assigned To filter
            if (!string.IsNullOrWhiteSpace(assignedTo))
            {
                query = query.Where(l => l.AssignedTo == assignedTo);
            }

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            if (totalPages < 1) totalPages = 1;
            if (pageIndex < 1) pageIndex = 1;
            if (pageIndex > totalPages) pageIndex = totalPages;

            var leads = await query
                .OrderByDescending(l => l.CreatedDate)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var salesExecutives = await GetSalesExecutivesAsync();

            return new LeadListViewModel
            {
                Leads = leads,
                SearchTerm = search,
                StatusFilter = status,
                PriorityFilter = priority,
                AssignedToFilter = assignedTo,
                PageIndex = pageIndex,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                SalesExecutives = salesExecutives
            };
        }

        public async Task<Lead?> GetLeadByIdAsync(int id, string userId, string role)
        {
            var query = _context.Leads
                .Include(l => l.AssignedUser)
                .Include(l => l.ConvertedCustomer)
                .Include(l => l.ConvertedOpportunity)
                .AsQueryable();

            if (role == AppRoles.SalesExecutive)
            {
                query = query.Where(l => l.AssignedTo == userId);
            }

            return await query.FirstOrDefaultAsync(l => l.LeadId == id);
        }

        public async Task<LeadDetailsViewModel?> GetLeadDetailsAsync(int id, string userId, string role)
        {
            var lead = await GetLeadByIdAsync(id, userId, role);
            if (lead == null) return null;

            var opportunities = await _context.Opportunities
                .Where(o => o.LeadId == id)
                .OrderByDescending(o => o.CreatedDate)
                .ToListAsync();

            var followUps = await _context.FollowUps
                .Where(f => f.LeadId == id)
                .OrderByDescending(f => f.FollowUpDate)
                .ToListAsync();

            var activities = await _context.Activities
                .Where(a => a.LeadId == id)
                .OrderByDescending(a => a.ActivityDate)
                .ToListAsync();

            var auditLogs = await _context.AuditLogs
                .Where(a => a.EntityName == "Lead" && a.RecordId == id.ToString())
                .OrderByDescending(a => a.CreatedDate)
                .ToListAsync();

            return new LeadDetailsViewModel
            {
                Lead = lead,
                Opportunities = opportunities,
                FollowUps = followUps,
                Activities = activities,
                AuditHistory = auditLogs
            };
        }

        public async Task<bool> CreateLeadAsync(Lead lead, string currentUserId)
        {
            if (string.IsNullOrWhiteSpace(lead.LeadCode))
            {
                lead.LeadCode = await GenerateLeadCodeAsync();
            }

            lead.CreatedDate = DateTime.UtcNow;

            _context.Leads.Add(lead);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                userId: currentUserId,
                action: "LeadCreated",
                entityName: "Lead",
                recordId: lead.LeadId.ToString(),
                oldValue: null,
                newValue: $"Code: {lead.LeadCode}, Name: {lead.LeadName}, Email: {lead.Email}, Status: {lead.Status}, Value: {lead.ExpectedValue}",
                result: "Success",
                details: $"Lead '{lead.LeadName}' created with status '{lead.Status}'.");

            return true;
        }

        public async Task<bool> UpdateLeadAsync(Lead lead, string currentUserId)
        {
            var existing = await _context.Leads.FindAsync(lead.LeadId);
            if (existing == null) return false;

            var oldValue = $"Name: {existing.LeadName}, Email: {existing.Email}, Phone: {existing.Phone}, Status: {existing.Status}, Priority: {existing.Priority}, Value: {existing.ExpectedValue}, AssignedTo: {existing.AssignedTo}";

            existing.LeadName = lead.LeadName;
            existing.Email = lead.Email;
            existing.Phone = lead.Phone;
            existing.CompanyName = lead.CompanyName;
            existing.Source = lead.Source;
            existing.Status = lead.Status;
            existing.Priority = lead.Priority;
            existing.ExpectedValue = lead.ExpectedValue;
            existing.AssignedTo = lead.AssignedTo;

            await _context.SaveChangesAsync();

            var newValue = $"Name: {existing.LeadName}, Email: {existing.Email}, Phone: {existing.Phone}, Status: {existing.Status}, Priority: {existing.Priority}, Value: {existing.ExpectedValue}, AssignedTo: {existing.AssignedTo}";

            await _auditService.LogAsync(
                userId: currentUserId,
                action: "LeadUpdated",
                entityName: "Lead",
                recordId: existing.LeadId.ToString(),
                oldValue: oldValue,
                newValue: newValue,
                result: "Success",
                details: $"Lead '{existing.LeadName}' updated.");

            return true;
        }

        public async Task<bool> UpdateStatusAsync(int leadId, string newStatus, string currentUserId)
        {
            var lead = await _context.Leads.FindAsync(leadId);
            if (lead == null) return false;

            if (lead.Status == LeadStatuses.Converted && newStatus != LeadStatuses.Converted)
            {
                // Converted leads cannot revert
                return false;
            }

            var oldStatus = lead.Status;
            lead.Status = newStatus;
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                userId: currentUserId,
                action: "LeadStatusChanged",
                entityName: "Lead",
                recordId: lead.LeadId.ToString(),
                oldValue: $"Status: {oldStatus}",
                newValue: $"Status: {newStatus}",
                result: "Success",
                details: $"Lead '{lead.LeadName}' status transitioned from '{oldStatus}' to '{newStatus}'.");

            return true;
        }

        public async Task<bool> DeleteLeadAsync(int leadId, string currentUserId)
        {
            var lead = await _context.Leads.FindAsync(leadId);
            if (lead == null) return false;

            _context.Leads.Remove(lead);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                userId: currentUserId,
                action: "LeadDeleted",
                entityName: "Lead",
                recordId: leadId.ToString(),
                oldValue: $"Code: {lead.LeadCode}, Name: {lead.LeadName}",
                newValue: null,
                result: "Success",
                details: $"Lead '{lead.LeadName}' deleted.");

            return true;
        }

        public async Task<string> GenerateLeadCodeAsync()
        {
            var count = await _context.Leads.CountAsync();
            return $"LEAD-{(2000 + count + 1)}";
        }

        public async Task<(bool Success, string Message, int? CustomerId, int? OpportunityId)> ConvertLeadAsync(
            LeadConvertViewModel model,
            string currentUserId)
        {
            var lead = await _context.Leads.FindAsync(model.LeadId);
            if (lead == null)
            {
                return (false, "Lead not found.", null, null);
            }

            if (lead.Status == LeadStatuses.Converted)
            {
                return (false, "This lead has already been converted.", lead.ConvertedCustomerId, lead.ConvertedOpportunityId);
            }

            // 1. Find or create Customer
            Customer? customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Email.ToLower() == model.Email.Trim().ToLower() || c.Phone == model.Phone.Trim());

            if (customer == null)
            {
                var custCount = await _context.Customers.CountAsync();
                var customerCode = $"CUST-{(1000 + custCount + 1)}";

                customer = new Customer
                {
                    CustomerCode = customerCode,
                    CustomerName = model.CustomerName.Trim(),
                    Email = model.Email.Trim(),
                    Phone = model.Phone.Trim(),
                    CompanyName = model.CompanyName?.Trim(),
                    Address = model.Address?.Trim(),
                    City = model.City?.Trim(),
                    State = model.State?.Trim(),
                    Status = CustomerStatuses.Active,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = currentUserId,
                    AssignedTo = model.AssignedTo ?? lead.AssignedTo ?? currentUserId
                };

                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();

                await _auditService.LogAsync(
                    userId: currentUserId,
                    action: "CustomerCreated",
                    entityName: "Customer",
                    recordId: customer.CustomerId.ToString(),
                    oldValue: null,
                    newValue: $"Code: {customer.CustomerCode}, Name: {customer.CustomerName}",
                    result: "Success",
                    details: $"Customer '{customer.CustomerName}' created via Lead Conversion (Lead #{lead.LeadId}).");
            }

            Opportunity? opportunity = null;
            if (model.CreateOpportunity)
            {
                if (model.OpportunityAmount <= 0)
                {
                    return (false, "Opportunity Amount must be greater than 0.", null, null);
                }

                if (model.Probability < 0 || model.Probability > 100)
                {
                    return (false, "Probability must be between 0 and 100.", null, null);
                }

                if (model.ExpectedCloseDate.Date < DateTime.Today)
                {
                    return (false, "Expected Close Date cannot be in the past.", null, null);
                }

                var oppName = !string.IsNullOrWhiteSpace(model.OpportunityName)
                    ? model.OpportunityName.Trim()
                    : $"Deal with {customer.CompanyName ?? customer.CustomerName}";

                opportunity = new Opportunity
                {
                    OpportunityName = oppName,
                    CustomerId = customer.CustomerId,
                    LeadId = lead.LeadId,
                    Amount = model.OpportunityAmount,
                    Stage = model.OpportunityStage,
                    Probability = model.Probability,
                    ExpectedCloseDate = model.ExpectedCloseDate,
                    Status = OpportunityStatuses.Open,
                    CreatedDate = DateTime.UtcNow,
                    AssignedTo = model.AssignedTo ?? lead.AssignedTo ?? currentUserId
                };

                _context.Opportunities.Add(opportunity);
                await _context.SaveChangesAsync();

                await _auditService.LogAsync(
                    userId: currentUserId,
                    action: "OpportunityCreated",
                    entityName: "Opportunity",
                    recordId: opportunity.OpportunityId.ToString(),
                    oldValue: null,
                    newValue: $"Name: {opportunity.OpportunityName}, Amount: {opportunity.Amount}, Stage: {opportunity.Stage}",
                    result: "Success",
                    details: $"Opportunity '{opportunity.OpportunityName}' created via Lead Conversion (Lead #{lead.LeadId}).");
            }

            // Update Lead to Converted
            lead.Status = LeadStatuses.Converted;
            lead.ConvertedCustomerId = customer.CustomerId;
            if (opportunity != null)
            {
                lead.ConvertedOpportunityId = opportunity.OpportunityId;
            }

            // Link existing follow-ups and activities to the new customer as well
            var leadFollowUps = await _context.FollowUps.Where(f => f.LeadId == lead.LeadId && f.CustomerId == null).ToListAsync();
            foreach (var fu in leadFollowUps)
            {
                fu.CustomerId = customer.CustomerId;
            }

            var leadActivities = await _context.Activities.Where(a => a.LeadId == lead.LeadId && a.CustomerId == null).ToListAsync();
            foreach (var act in leadActivities)
            {
                act.CustomerId = customer.CustomerId;
            }

            await _context.SaveChangesAsync();

            // Centralized Lead Conversion Audit Log
            await _auditService.LogAsync(
                userId: currentUserId,
                action: "LeadConverted",
                entityName: "Lead",
                recordId: lead.LeadId.ToString(),
                oldValue: "Status: Qualified",
                newValue: $"Converted to Customer #{customer.CustomerId} ({customer.CustomerName})" + (opportunity != null ? $", Opportunity #{opportunity.OpportunityId} ({opportunity.OpportunityName})" : ""),
                result: "Success",
                details: $"Lead '{lead.LeadName}' successfully converted to Customer #{customer.CustomerId}" + (opportunity != null ? $" and Opportunity #{opportunity.OpportunityId}" : "") + ".");

            return (true, "Lead successfully converted!", customer.CustomerId, opportunity?.OpportunityId);
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
