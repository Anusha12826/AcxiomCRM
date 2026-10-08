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
    public class ActivitiesController : Controller
    {
        private readonly IActivityService _activityService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ActivitiesController(
            IActivityService activityService,
            UserManager<ApplicationUser> userManager)
        {
            _activityService = activityService;
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

        private async Task PopulateDropdownsAsync(string userId, string role)
        {
            ViewBag.Customers = await _activityService.GetCustomersDropdownAsync(userId, role);
            ViewBag.Leads = await _activityService.GetLeadsDropdownAsync(userId, role);
            ViewBag.SalesExecutives = await _activityService.GetSalesExecutivesAsync();
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string? search = null,
            string? type = null,
            string? status = null,
            string? assignedTo = null,
            int page = 1)
        {
            var (userId, role) = await GetUserContextAsync();
            var model = await _activityService.GetActivitiesAsync(userId, role, search, type, status, assignedTo, page, 10);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var (userId, role) = await GetUserContextAsync();
            var activity = await _activityService.GetActivityByIdAsync(id, userId, role);
            if (activity == null)
            {
                TempData["ErrorMessage"] = "Activity record not found or access is unauthorized.";
                return RedirectToAction(nameof(Index));
            }

            return View(activity);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int? customerId = null, int? leadId = null)
        {
            var (userId, role) = await GetUserContextAsync();
            await PopulateDropdownsAsync(userId, role);

            var model = new ActivityCreateViewModel
            {
                CustomerId = customerId,
                LeadId = leadId,
                ActivityDate = DateTime.Now,
                AssignedTo = userId
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ActivityCreateViewModel model)
        {
            var (userId, role) = await GetUserContextAsync();
            await PopulateDropdownsAsync(userId, role);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var activity = new Activity
            {
                ActivityType = model.ActivityType,
                Subject = model.Subject.Trim(),
                Description = model.Description?.Trim(),
                ActivityDate = model.ActivityDate,
                CustomerId = model.CustomerId,
                LeadId = model.LeadId,
                Status = model.Status,
                AssignedTo = !string.IsNullOrEmpty(model.AssignedTo) ? model.AssignedTo : userId
            };

            var success = await _activityService.CreateActivityAsync(activity, userId);
            if (success)
            {
                TempData["SuccessMessage"] = $"Activity '{activity.Subject}' recorded successfully.";
                return RedirectToAction(nameof(Index));
            }

            TempData["ErrorMessage"] = "Failed to record activity.";
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var (userId, role) = await GetUserContextAsync();
            var activity = await _activityService.GetActivityByIdAsync(id, userId, role);
            if (activity == null)
            {
                TempData["ErrorMessage"] = "Activity not found or access unauthorized.";
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdownsAsync(userId, role);

            var model = new ActivityEditViewModel
            {
                ActivityId = activity.ActivityId,
                ActivityType = activity.ActivityType,
                Subject = activity.Subject,
                Description = activity.Description,
                ActivityDate = activity.ActivityDate,
                CustomerId = activity.CustomerId,
                LeadId = activity.LeadId,
                Status = activity.Status,
                AssignedTo = activity.AssignedTo
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ActivityEditViewModel model)
        {
            if (id != model.ActivityId) return BadRequest();

            var (userId, role) = await GetUserContextAsync();
            await PopulateDropdownsAsync(userId, role);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var activity = new Activity
            {
                ActivityId = model.ActivityId,
                ActivityType = model.ActivityType,
                Subject = model.Subject.Trim(),
                Description = model.Description?.Trim(),
                ActivityDate = model.ActivityDate,
                CustomerId = model.CustomerId,
                LeadId = model.LeadId,
                Status = model.Status,
                AssignedTo = model.AssignedTo
            };

            var success = await _activityService.UpdateActivityAsync(activity, userId);
            if (success)
            {
                TempData["SuccessMessage"] = $"Activity '{activity.Subject}' updated successfully.";
                return RedirectToAction(nameof(Details), new { id = activity.ActivityId });
            }

            TempData["ErrorMessage"] = "Failed to update activity.";
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string newStatus)
        {
            var (userId, _) = await GetUserContextAsync();

            var success = await _activityService.UpdateStatusAsync(id, newStatus, userId);
            if (success)
            {
                TempData["SuccessMessage"] = $"Activity status updated to '{newStatus}'.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to update activity status.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var (userId, role) = await GetUserContextAsync();

            if (role != AppRoles.Admin && role != AppRoles.Manager)
            {
                TempData["ErrorMessage"] = "Only Administrators and Managers are permitted to delete activity records.";
                return RedirectToAction(nameof(Index));
            }

            var success = await _activityService.DeleteActivityAsync(id, userId);
            if (success)
            {
                TempData["SuccessMessage"] = "Activity record deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to delete activity record.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
