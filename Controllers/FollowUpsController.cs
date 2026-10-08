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
    public class FollowUpsController : Controller
    {
        private readonly IFollowUpService _followUpService;
        private readonly UserManager<ApplicationUser> _userManager;

        public FollowUpsController(
            IFollowUpService followUpService,
            UserManager<ApplicationUser> userManager)
        {
            _followUpService = followUpService;
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
            ViewBag.Customers = await _followUpService.GetCustomersDropdownAsync(userId, role);
            ViewBag.Leads = await _followUpService.GetLeadsDropdownAsync(userId, role);
            ViewBag.SalesExecutives = await _followUpService.GetSalesExecutivesAsync();
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string? tab = "all",
            string? type = null,
            string? assignedTo = null,
            int page = 1)
        {
            var (userId, role) = await GetUserContextAsync();
            var model = await _followUpService.GetFollowUpsAsync(userId, role, tab, type, assignedTo, page, 10);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var (userId, role) = await GetUserContextAsync();
            var followUp = await _followUpService.GetFollowUpByIdAsync(id, userId, role);
            if (followUp == null)
            {
                TempData["ErrorMessage"] = "Follow-up record not found or access is unauthorized.";
                return RedirectToAction(nameof(Index));
            }

            return View(followUp);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int? customerId = null, int? leadId = null)
        {
            var (userId, role) = await GetUserContextAsync();
            await PopulateDropdownsAsync(userId, role);

            var model = new FollowUpCreateViewModel
            {
                CustomerId = customerId,
                LeadId = leadId,
                FollowUpDate = DateTime.Today.AddDays(1).AddHours(10),
                AssignedTo = userId
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FollowUpCreateViewModel model)
        {
            var (userId, role) = await GetUserContextAsync();
            await PopulateDropdownsAsync(userId, role);

            // Server-Side Business Validation: Follow-up date cannot be earlier than today
            if (model.FollowUpDate.Date < DateTime.Today)
            {
                ModelState.AddModelError(nameof(model.FollowUpDate), "Follow-up date cannot be earlier than today.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var followUp = new FollowUp
            {
                CustomerId = model.CustomerId,
                LeadId = model.LeadId,
                OpportunityId = model.OpportunityId,
                FollowUpDate = model.FollowUpDate,
                FollowUpType = model.FollowUpType,
                Remarks = model.Remarks?.Trim(),
                AssignedTo = !string.IsNullOrEmpty(model.AssignedTo) ? model.AssignedTo : userId
            };

            var success = await _followUpService.CreateFollowUpAsync(followUp, userId);
            if (success)
            {
                TempData["SuccessMessage"] = "Follow-up scheduled successfully.";
                return RedirectToAction(nameof(Index));
            }

            TempData["ErrorMessage"] = "Failed to schedule follow-up.";
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var (userId, role) = await GetUserContextAsync();
            var followUp = await _followUpService.GetFollowUpByIdAsync(id, userId, role);
            if (followUp == null)
            {
                TempData["ErrorMessage"] = "Follow-up not found or unauthorized.";
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdownsAsync(userId, role);

            var model = new FollowUpEditViewModel
            {
                FollowUpId = followUp.FollowUpId,
                CustomerId = followUp.CustomerId,
                LeadId = followUp.LeadId,
                OpportunityId = followUp.OpportunityId,
                FollowUpDate = followUp.FollowUpDate,
                FollowUpType = followUp.FollowUpType,
                Status = followUp.Status,
                Remarks = followUp.Remarks,
                AssignedTo = followUp.AssignedTo
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, FollowUpEditViewModel model)
        {
            if (id != model.FollowUpId) return BadRequest();

            var (userId, role) = await GetUserContextAsync();
            await PopulateDropdownsAsync(userId, role);

            // Business validation: Follow-up date cannot be earlier than today for planned follow-up
            if (model.Status == FollowUpStatuses.Planned && model.FollowUpDate.Date < DateTime.Today)
            {
                ModelState.AddModelError(nameof(model.FollowUpDate), "Follow-up date cannot be earlier than today for planned follow-ups.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var followUp = new FollowUp
            {
                FollowUpId = model.FollowUpId,
                CustomerId = model.CustomerId,
                LeadId = model.LeadId,
                OpportunityId = model.OpportunityId,
                FollowUpDate = model.FollowUpDate,
                FollowUpType = model.FollowUpType,
                Status = model.Status,
                Remarks = model.Remarks?.Trim(),
                AssignedTo = model.AssignedTo
            };

            var success = await _followUpService.UpdateFollowUpAsync(followUp, userId);
            if (success)
            {
                TempData["SuccessMessage"] = "Follow-up updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            TempData["ErrorMessage"] = "Failed to update follow-up.";
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(int id, string completionRemarks)
        {
            if (string.IsNullOrWhiteSpace(completionRemarks))
            {
                TempData["ErrorMessage"] = "Please provide completion outcome remarks.";
                return RedirectToAction(nameof(Index));
            }

            var (userId, _) = await GetUserContextAsync();
            var success = await _followUpService.CompleteFollowUpAsync(id, completionRemarks.Trim(), userId);
            if (success)
            {
                TempData["SuccessMessage"] = "Follow-up marked as Completed successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to mark follow-up as completed.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reschedule(int id, DateTime newDate, string? reason)
        {
            if (newDate.Date < DateTime.Today)
            {
                TempData["ErrorMessage"] = "Follow-up date cannot be earlier than today.";
                return RedirectToAction(nameof(Index));
            }

            var (userId, _) = await GetUserContextAsync();
            var success = await _followUpService.RescheduleFollowUpAsync(id, newDate, reason, userId);
            if (success)
            {
                TempData["SuccessMessage"] = $"Follow-up successfully rescheduled to {newDate:MMM dd, yyyy HH:mm}.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to reschedule follow-up.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id, string? reason)
        {
            var (userId, _) = await GetUserContextAsync();
            var success = await _followUpService.CancelFollowUpAsync(id, reason, userId);
            if (success)
            {
                TempData["SuccessMessage"] = "Follow-up cancelled.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to cancel follow-up.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var (userId, role) = await GetUserContextAsync();

            if (role != AppRoles.Admin && role != AppRoles.Manager)
            {
                TempData["ErrorMessage"] = "Only Administrators and Managers are permitted to delete follow-ups.";
                return RedirectToAction(nameof(Index));
            }

            var success = await _followUpService.DeleteFollowUpAsync(id, userId);
            if (success)
            {
                TempData["SuccessMessage"] = "Follow-up deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to delete follow-up.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
