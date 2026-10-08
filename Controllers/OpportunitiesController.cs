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
    public class OpportunitiesController : Controller
    {
        private readonly IOpportunityService _opportunityService;
        private readonly UserManager<ApplicationUser> _userManager;

        public OpportunitiesController(
            IOpportunityService opportunityService,
            UserManager<ApplicationUser> userManager)
        {
            _opportunityService = opportunityService;
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
            ViewBag.Customers = await _opportunityService.GetCustomersDropdownAsync(userId, role);
            ViewBag.Leads = await _opportunityService.GetLeadsDropdownAsync(userId, role);
            ViewBag.SalesExecutives = await _opportunityService.GetSalesExecutivesAsync();
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string? search = null,
            string? stage = null,
            string? status = null,
            string? assignedTo = null,
            int page = 1)
        {
            var (userId, role) = await GetUserContextAsync();
            var model = await _opportunityService.GetOpportunitiesAsync(userId, role, search, stage, status, assignedTo, page, 10);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var (userId, role) = await GetUserContextAsync();
            var model = await _opportunityService.GetOpportunityDetailsAsync(id, userId, role);
            if (model == null)
            {
                TempData["ErrorMessage"] = "Opportunity record not found or access is unauthorized.";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int? customerId = null, int? leadId = null)
        {
            var (userId, role) = await GetUserContextAsync();
            await PopulateDropdownsAsync(userId, role);

            var model = new OpportunityCreateViewModel
            {
                CustomerId = customerId,
                LeadId = leadId,
                ExpectedCloseDate = DateTime.Today.AddDays(30),
                AssignedTo = userId
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OpportunityCreateViewModel model)
        {
            var (userId, role) = await GetUserContextAsync();
            await PopulateDropdownsAsync(userId, role);

            // Mandatory Business Validation Rules:
            if (model.Amount <= 0)
            {
                ModelState.AddModelError(nameof(model.Amount), "Opportunity Amount must be greater than 0.");
            }

            if (model.Probability < 0 || model.Probability > 100)
            {
                ModelState.AddModelError(nameof(model.Probability), "Probability must be between 0 and 100.");
            }

            if (model.ExpectedCloseDate.Date < DateTime.Today)
            {
                ModelState.AddModelError(nameof(model.ExpectedCloseDate), "Expected Close Date cannot be in the past.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var opp = new Opportunity
            {
                OpportunityName = model.OpportunityName.Trim(),
                CustomerId = model.CustomerId,
                LeadId = model.LeadId,
                Amount = model.Amount,
                Stage = model.Stage,
                Probability = model.Probability,
                ExpectedCloseDate = model.ExpectedCloseDate,
                AssignedTo = !string.IsNullOrEmpty(model.AssignedTo) ? model.AssignedTo : userId
            };

            var success = await _opportunityService.CreateOpportunityAsync(opp, userId);
            if (success)
            {
                TempData["SuccessMessage"] = $"Opportunity '{opp.OpportunityName}' created successfully.";
                return RedirectToAction(nameof(Index));
            }

            TempData["ErrorMessage"] = "Failed to create opportunity.";
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var (userId, role) = await GetUserContextAsync();
            var opp = await _opportunityService.GetOpportunityByIdAsync(id, userId, role);
            if (opp == null)
            {
                TempData["ErrorMessage"] = "Opportunity not found or access is unauthorized.";
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdownsAsync(userId, role);

            var model = new OpportunityEditViewModel
            {
                OpportunityId = opp.OpportunityId,
                OpportunityName = opp.OpportunityName,
                CustomerId = opp.CustomerId,
                LeadId = opp.LeadId,
                Amount = opp.Amount,
                Stage = opp.Stage,
                Probability = opp.Probability,
                ExpectedCloseDate = opp.ExpectedCloseDate,
                Status = opp.Status,
                AssignedTo = opp.AssignedTo
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, OpportunityEditViewModel model)
        {
            if (id != model.OpportunityId) return BadRequest();

            var (userId, role) = await GetUserContextAsync();
            await PopulateDropdownsAsync(userId, role);

            // Mandatory Business Validation Rules:
            if (model.Status == OpportunityStatuses.Open && model.Amount <= 0)
            {
                ModelState.AddModelError(nameof(model.Amount), "Opportunity Amount must be greater than 0.");
            }

            if (model.Probability < 0 || model.Probability > 100)
            {
                ModelState.AddModelError(nameof(model.Probability), "Probability must be between 0 and 100.");
            }

            if (model.Status == OpportunityStatuses.Open && model.ExpectedCloseDate.Date < DateTime.Today)
            {
                ModelState.AddModelError(nameof(model.ExpectedCloseDate), "Expected Close Date cannot be in the past for active opportunities.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var opp = new Opportunity
            {
                OpportunityId = model.OpportunityId,
                OpportunityName = model.OpportunityName.Trim(),
                CustomerId = model.CustomerId,
                LeadId = model.LeadId,
                Amount = model.Amount,
                Stage = model.Stage,
                Probability = model.Probability,
                ExpectedCloseDate = model.ExpectedCloseDate,
                Status = model.Status,
                AssignedTo = model.AssignedTo
            };

            var success = await _opportunityService.UpdateOpportunityAsync(opp, userId);
            if (success)
            {
                TempData["SuccessMessage"] = $"Opportunity '{opp.OpportunityName}' updated successfully.";
                return RedirectToAction(nameof(Details), new { id = opp.OpportunityId });
            }

            TempData["ErrorMessage"] = "Failed to update opportunity.";
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStage(int id, string newStage)
        {
            var (userId, _) = await GetUserContextAsync();

            if (!OpportunityStages.AllStages.Contains(newStage))
            {
                TempData["ErrorMessage"] = "Invalid pipeline stage specified.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var success = await _opportunityService.UpdateStageAsync(id, newStage, userId);
            if (success)
            {
                TempData["SuccessMessage"] = $"Opportunity pipeline stage updated to '{newStage}'.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to transition pipeline stage.";
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
                TempData["ErrorMessage"] = "Only Administrators and Managers are permitted to delete opportunities.";
                return RedirectToAction(nameof(Index));
            }

            var success = await _opportunityService.DeleteOpportunityAsync(id, userId);
            if (success)
            {
                TempData["SuccessMessage"] = "Opportunity deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to delete opportunity.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
