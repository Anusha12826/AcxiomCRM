using AcxiomCRM.Models;
using AcxiomCRM.Models.Constants;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers
{
    [Authorize]
    public class CustomersController : Controller
    {
        private readonly ICustomerService _customerService;
        private readonly UserManager<ApplicationUser> _userManager;

        public CustomersController(
            ICustomerService customerService,
            UserManager<ApplicationUser> userManager)
        {
            _customerService = customerService;
            _userManager = userManager;
        }

        private async Task<(string UserId, string Role)> GetUserContextAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return (string.Empty, AppRoles.SalesExecutive);

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? AppRoles.SalesExecutive;
            return (user.Id, role);
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string? search = null,
            string? status = null,
            string? assignedTo = null,
            int page = 1)
        {
            var (userId, role) = await GetUserContextAsync();
            var model = await _customerService.GetCustomersAsync(userId, role, search, status, assignedTo, page, 10);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var (userId, role) = await GetUserContextAsync();
            var model = await _customerService.GetCustomerDetailsAsync(id, userId, role);
            if (model == null)
            {
                TempData["ErrorMessage"] = "Customer not found or you are not authorized to view this record.";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.SalesExecutives = await _customerService.GetSalesExecutivesAsync();
            var suggestedCode = await _customerService.GenerateCustomerCodeAsync();

            return View(new CustomerCreateViewModel
            {
                CustomerCode = suggestedCode
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CustomerCreateViewModel model)
        {
            var (userId, role) = await GetUserContextAsync();
            ViewBag.SalesExecutives = await _customerService.GetSalesExecutivesAsync();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Server-Side Uniqueness Validations (Mandatory Requirement)
            if (!await _customerService.IsEmailUniqueAsync(model.Email))
            {
                ModelState.AddModelError(nameof(model.Email), "A customer with this email address already exists.");
                return View(model);
            }

            if (!await _customerService.IsPhoneUniqueAsync(model.Phone))
            {
                ModelState.AddModelError(nameof(model.Phone), "A customer with this phone number already exists.");
                return View(model);
            }

            var customer = new Customer
            {
                CustomerCode = model.CustomerCode?.Trim() ?? await _customerService.GenerateCustomerCodeAsync(),
                CustomerName = model.CustomerName.Trim(),
                Email = model.Email.Trim(),
                Phone = model.Phone.Trim(),
                CompanyName = model.CompanyName?.Trim(),
                Address = model.Address?.Trim(),
                City = model.City?.Trim(),
                State = model.State?.Trim(),
                Status = CustomerStatuses.Active,
                AssignedTo = !string.IsNullOrEmpty(model.AssignedTo) ? model.AssignedTo : userId
            };

            var success = await _customerService.CreateCustomerAsync(customer, userId);
            if (success)
            {
                TempData["SuccessMessage"] = $"Customer '{customer.CustomerName}' created successfully.";
                return RedirectToAction(nameof(Index));
            }

            TempData["ErrorMessage"] = "Failed to create customer record. Please try again.";
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var (userId, role) = await GetUserContextAsync();
            var customer = await _customerService.GetCustomerByIdAsync(id, userId, role);
            if (customer == null)
            {
                TempData["ErrorMessage"] = "Customer not found or access is unauthorized.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.SalesExecutives = await _customerService.GetSalesExecutivesAsync();

            var model = new CustomerEditViewModel
            {
                CustomerId = customer.CustomerId,
                CustomerCode = customer.CustomerCode,
                CustomerName = customer.CustomerName,
                Email = customer.Email,
                Phone = customer.Phone,
                CompanyName = customer.CompanyName,
                Address = customer.Address,
                City = customer.City,
                State = customer.State,
                Status = customer.Status,
                AssignedTo = customer.AssignedTo
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CustomerEditViewModel model)
        {
            if (id != model.CustomerId) return BadRequest();

            var (userId, role) = await GetUserContextAsync();
            ViewBag.SalesExecutives = await _customerService.GetSalesExecutivesAsync();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Server-Side Uniqueness Checks
            if (!await _customerService.IsEmailUniqueAsync(model.Email, id))
            {
                ModelState.AddModelError(nameof(model.Email), "A customer with this email address already exists.");
                return View(model);
            }

            if (!await _customerService.IsPhoneUniqueAsync(model.Phone, id))
            {
                ModelState.AddModelError(nameof(model.Phone), "A customer with this phone number already exists.");
                return View(model);
            }

            var customer = new Customer
            {
                CustomerId = model.CustomerId,
                CustomerCode = model.CustomerCode.Trim(),
                CustomerName = model.CustomerName.Trim(),
                Email = model.Email.Trim(),
                Phone = model.Phone.Trim(),
                CompanyName = model.CompanyName?.Trim(),
                Address = model.Address?.Trim(),
                City = model.City?.Trim(),
                State = model.State?.Trim(),
                Status = model.Status,
                AssignedTo = model.AssignedTo
            };

            var success = await _customerService.UpdateCustomerAsync(customer, userId);
            if (success)
            {
                TempData["SuccessMessage"] = $"Customer '{customer.CustomerName}' updated successfully.";
                return RedirectToAction(nameof(Details), new { id = customer.CustomerId });
            }

            TempData["ErrorMessage"] = "Failed to update customer.";
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(int id)
        {
            var (userId, role) = await GetUserContextAsync();
            var success = await _customerService.DeactivateCustomerAsync(id, userId);

            if (success)
            {
                TempData["SuccessMessage"] = "Customer deactivated successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to deactivate customer.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var (userId, role) = await GetUserContextAsync();

            // Only Admin or Manager can hard-delete records
            if (role != AppRoles.Admin && role != AppRoles.Manager)
            {
                TempData["ErrorMessage"] = "Only Administrators and Managers are permitted to delete customer records.";
                return RedirectToAction(nameof(Index));
            }

            var success = await _customerService.DeleteCustomerAsync(id, userId);
            if (success)
            {
                TempData["SuccessMessage"] = "Customer deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to delete customer.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
