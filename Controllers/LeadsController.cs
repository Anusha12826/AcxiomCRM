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
    public class LeadsController : Controller
    {
        private readonly ILeadService _leadService;
        private readonly UserManager<ApplicationUser> _userManager;

        public LeadsController(
            ILeadService leadService,
            UserManager<ApplicationUser> userManager)
        {
            _leadService = leadService;
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
            string? priority = null,
            string? assignedTo = null,
            int page = 1)
        {
            var (userId, role) = await GetUserContextAsync();
            var model = await _leadService.GetLeadsAsync(userId, role, search, status, priority, assignedTo, page, 10);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var (userId, role) = await GetUserContextAsync();
            var model = await _leadService.GetLeadDetailsAsync(id, userId, role);
            if (model == null)
            {
                TempData["ErrorMessage"] = "Lead not found or access is unauthorized.";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.SalesExecutives = await _leadService.GetSalesExecutivesAsync();
            var code = await _leadService.GenerateLeadCodeAsync();

            return View(new LeadCreateViewModel
            {
                LeadCode = code
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LeadCreateViewModel model)
        {
            var (userId, _) = await GetUserContextAsync();
            ViewBag.SalesExecutives = await _leadService.GetSalesExecutivesAsync();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var lead = new Lead
            {
                LeadCode = model.LeadCode?.Trim() ?? await _leadService.GenerateLeadCodeAsync(),
                LeadName = model.LeadName.Trim(),
                Email = model.Email.Trim(),
                Phone = model.Phone.Trim(),
                CompanyName = model.CompanyName?.Trim(),
                Source = model.Source.Trim(),
                Priority = model.Priority,
                ExpectedValue = model.ExpectedValue,
                Status = LeadStatuses.New,
                AssignedTo = !string.IsNullOrEmpty(model.AssignedTo) ? model.AssignedTo : userId
            };

            var success = await _leadService.CreateLeadAsync(lead, userId);
            if (success)
            {
                TempData["SuccessMessage"] = $"Lead '{lead.LeadName}' created successfully.";
                return RedirectToAction(nameof(Index));
            }

            TempData["ErrorMessage"] = "Failed to create lead.";
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var (userId, role) = await GetUserContextAsync();
            var lead = await _leadService.GetLeadByIdAsync(id, userId, role);
            if (lead == null)
            {
                TempData["ErrorMessage"] = "Lead not found or access is unauthorized.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.SalesExecutives = await _leadService.GetSalesExecutivesAsync();

            var model = new LeadEditViewModel
            {
                LeadId = lead.LeadId,
                LeadCode = lead.LeadCode,
                LeadName = lead.LeadName,
                Email = lead.Email,
                Phone = lead.Phone,
                CompanyName = lead.CompanyName,
                Source = lead.Source,
                Status = lead.Status,
                Priority = lead.Priority ?? "Medium",
                ExpectedValue = lead.ExpectedValue,
                AssignedTo = lead.AssignedTo
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, LeadEditViewModel model)
        {
            if (id != model.LeadId) return BadRequest();

            var (userId, _) = await GetUserContextAsync();
            ViewBag.SalesExecutives = await _leadService.GetSalesExecutivesAsync();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var lead = new Lead
            {
                LeadId = model.LeadId,
                LeadCode = model.LeadCode.Trim(),
                LeadName = model.LeadName.Trim(),
                Email = model.Email.Trim(),
                Phone = model.Phone.Trim(),
                CompanyName = model.CompanyName?.Trim(),
                Source = model.Source.Trim(),
                Status = model.Status,
                Priority = model.Priority,
                ExpectedValue = model.ExpectedValue,
                AssignedTo = model.AssignedTo
            };

            var success = await _leadService.UpdateLeadAsync(lead, userId);
            if (success)
            {
                TempData["SuccessMessage"] = $"Lead '{lead.LeadName}' updated successfully.";
                return RedirectToAction(nameof(Details), new { id = lead.LeadId });
            }

            TempData["ErrorMessage"] = "Failed to update lead.";
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string newStatus)
        {
            var (userId, _) = await GetUserContextAsync();

            if (!LeadStatuses.AllStatuses.Contains(newStatus))
            {
                TempData["ErrorMessage"] = "Invalid status specified.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (newStatus == LeadStatuses.Converted)
            {
                return RedirectToAction(nameof(Convert), new { id });
            }

            var success = await _leadService.UpdateStatusAsync(id, newStatus, userId);
            if (success)
            {
                TempData["SuccessMessage"] = $"Lead status updated to '{newStatus}'.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to update status. Converted leads cannot be transitioned.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpGet]
        public async Task<IActionResult> Convert(int id)
        {
            var (userId, role) = await GetUserContextAsync();
            var lead = await _leadService.GetLeadByIdAsync(id, userId, role);
            if (lead == null)
            {
                TempData["ErrorMessage"] = "Lead not found.";
                return RedirectToAction(nameof(Index));
            }

            if (lead.Status == LeadStatuses.Converted)
            {
                TempData["InfoMessage"] = "This lead is already converted.";
                return RedirectToAction(nameof(Details), new { id });
            }

            ViewBag.SalesExecutives = await _leadService.GetSalesExecutivesAsync();

            var model = new LeadConvertViewModel
            {
                LeadId = lead.LeadId,
                LeadCode = lead.LeadCode,
                CustomerName = lead.LeadName,
                Email = lead.Email,
                Phone = lead.Phone,
                CompanyName = lead.CompanyName,
                CreateOpportunity = true,
                OpportunityName = $"Deal with {lead.CompanyName ?? lead.LeadName}",
                OpportunityAmount = lead.ExpectedValue > 0 ? lead.ExpectedValue : 100000m,
                OpportunityStage = OpportunityStages.Qualification,
                Probability = 40,
                ExpectedCloseDate = DateTime.Today.AddDays(30),
                AssignedTo = lead.AssignedTo
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Convert(int id, LeadConvertViewModel model)
        {
            if (id != model.LeadId) return BadRequest();

            var (userId, _) = await GetUserContextAsync();
            ViewBag.SalesExecutives = await _leadService.GetSalesExecutivesAsync();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _leadService.ConvertLeadAsync(model, userId);
            if (result.Success)
            {
                TempData["SuccessMessage"] = "Lead converted successfully! Customer account and Opportunity have been created.";
                if (result.CustomerId.HasValue)
                {
                    return RedirectToAction("Details", "Customers", new { id = result.CustomerId.Value });
                }
                return RedirectToAction(nameof(Details), new { id = model.LeadId });
            }

            ModelState.AddModelError(string.Empty, result.Message);
            TempData["ErrorMessage"] = result.Message;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var (userId, role) = await GetUserContextAsync();

            if (role != AppRoles.Admin && role != AppRoles.Manager)
            {
                TempData["ErrorMessage"] = "Only Administrators and Managers are permitted to delete leads.";
                return RedirectToAction(nameof(Index));
            }

            var success = await _leadService.DeleteLeadAsync(id, userId);
            if (success)
            {
                TempData["SuccessMessage"] = "Lead deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to delete lead.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
