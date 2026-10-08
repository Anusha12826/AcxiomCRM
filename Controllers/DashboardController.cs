using AcxiomCRM.Models;
using AcxiomCRM.Models.Constants;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly IDashboardService _dashboardService;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(
            IDashboardService dashboardService,
            UserManager<ApplicationUser> userManager)
        {
            _dashboardService = dashboardService;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string dateFilter = "All",
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? AppRoles.SalesExecutive;

            var model = await _dashboardService.GetDashboardMetricsAsync(
                user.Id,
                role,
                dateFilter,
                startDate,
                endDate);

            model.UserName = user.Email ?? user.UserName ?? string.Empty;
            model.UserFullName = string.IsNullOrEmpty(user.FullName) ? model.UserName : user.FullName;
            model.UserRole = role;

            return View(model);
        }
    }
}
