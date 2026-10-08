using AcxiomCRM.Models;
using AcxiomCRM.Models.Constants;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels.Account;
using AcxiomCRM.ViewModels.UserManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers
{
    [Authorize(Roles = AppRoles.Admin)]
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IAuditService _auditService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            IAuditService auditService,
            ILogger<UsersController> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _auditService = auditService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? search = null, string? roleFilter = null, bool? statusFilter = null)
        {
            var query = _userManager.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                query = query.Where(u => u.FullName.Contains(search) || u.Email!.Contains(search));
            }

            if (statusFilter.HasValue)
            {
                query = query.Where(u => u.IsActive == statusFilter.Value);
            }

            var users = await query.OrderByDescending(u => u.CreatedDate).ToListAsync();
            var userList = new List<UserListViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var primaryRole = roles.FirstOrDefault() ?? "None";

                if (!string.IsNullOrEmpty(roleFilter) && primaryRole != roleFilter)
                {
                    continue;
                }

                userList.Add(new UserListViewModel
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email ?? string.Empty,
                    Role = primaryRole,
                    IsActive = user.IsActive,
                    IsLockedOut = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow,
                    AccessFailedCount = user.AccessFailedCount,
                    CreatedDate = user.CreatedDate
                });
            }

            ViewData["Search"] = search;
            ViewData["RoleFilter"] = roleFilter;
            ViewData["StatusFilter"] = statusFilter;
            ViewBag.AllRoles = AppRoles.AllRoles;

            return View(userList);
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Roles = AppRoles.AllRoles;
            return View(new CreateUserViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            ViewBag.Roles = AppRoles.AllRoles;
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError(nameof(model.Email), "Email address is already registered.");
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                EmailConfirmed = true,
                IsActive = model.IsActive,
                CreatedDate = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                if (AppRoles.AllRoles.Contains(model.Role))
                {
                    await _userManager.AddToRoleAsync(user, model.Role);
                }

                var currentAdmin = await _userManager.GetUserAsync(User);
                await _auditService.LogAsync(
                    userId: currentAdmin?.Id,
                    action: "UserCreated",
                    entityName: "User",
                    recordId: user.Id,
                    oldValue: null,
                    newValue: $"Email: {user.Email}, Role: {model.Role}, Active: {user.IsActive}",
                    result: "Success",
                    details: $"Administrator created user '{user.Email}' with role '{model.Role}'.",
                    ipAddress: clientIp);

                TempData["SuccessMessage"] = $"User '{user.Email}' created successfully.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);
            var model = new EditUserViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                Role = roles.FirstOrDefault() ?? AppRoles.SalesExecutive,
                IsActive = user.IsActive
            };

            ViewBag.Roles = AppRoles.AllRoles;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditUserViewModel model)
        {
            ViewBag.Roles = AppRoles.AllRoles;
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null)
            {
                return NotFound();
            }

            var oldRoleList = await _userManager.GetRolesAsync(user);
            var oldRole = oldRoleList.FirstOrDefault() ?? "None";
            var oldValues = $"Name: {user.FullName}, Email: {user.Email}, Role: {oldRole}, Active: {user.IsActive}";

            user.FullName = model.FullName;
            user.Email = model.Email;
            user.UserName = model.Email;
            user.IsActive = model.IsActive;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

            // Update role if changed
            if (oldRole != model.Role && AppRoles.AllRoles.Contains(model.Role))
            {
                if (!string.IsNullOrEmpty(oldRole) && oldRole != "None")
                {
                    await _userManager.RemoveFromRoleAsync(user, oldRole);
                }
                await _userManager.AddToRoleAsync(user, model.Role);
            }

            var currentAdmin = await _userManager.GetUserAsync(User);
            var newValues = $"Name: {user.FullName}, Email: {user.Email}, Role: {model.Role}, Active: {user.IsActive}";

            await _auditService.LogAsync(
                userId: currentAdmin?.Id,
                action: "UserUpdated",
                entityName: "User",
                recordId: user.Id,
                oldValue: oldValues,
                newValue: newValues,
                result: "Success",
                details: $"Administrator updated profile and role for user '{user.Email}'.",
                ipAddress: clientIp);

            TempData["SuccessMessage"] = $"User '{user.Email}' updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var currentAdmin = await _userManager.GetUserAsync(User);
            if (currentAdmin != null && currentAdmin.Id == user.Id)
            {
                TempData["ErrorMessage"] = "You cannot deactivate your own account.";
                return RedirectToAction(nameof(Index));
            }

            user.IsActive = !user.IsActive;
            await _userManager.UpdateAsync(user);

            var actionName = user.IsActive ? "UserActivated" : "UserDeactivated";
            await _auditService.LogAsync(
                userId: currentAdmin?.Id,
                action: actionName,
                entityName: "User",
                recordId: user.Id,
                oldValue: $"IsActive: {!user.IsActive}",
                newValue: $"IsActive: {user.IsActive}",
                result: "Success",
                details: $"Administrator toggled user '{user.Email}' status to {(user.IsActive ? "Active" : "Inactive")}.",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["SuccessMessage"] = $"User '{user.Email}' is now {(user.IsActive ? "Active" : "Inactive")}.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unlock(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            await _userManager.SetLockoutEndDateAsync(user, null);
            await _userManager.ResetAccessFailedCountAsync(user);

            var currentAdmin = await _userManager.GetUserAsync(User);
            await _auditService.LogAsync(
                userId: currentAdmin?.Id,
                action: "UserUnlocked",
                entityName: "User",
                recordId: user.Id,
                oldValue: "LockedOut",
                newValue: "Unlocked",
                result: "Success",
                details: $"Administrator unlocked account for user '{user.Email}'.",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["SuccessMessage"] = $"User '{user.Email}' account has been unlocked.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> ResetPassword(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            return View(new ResetPasswordViewModel
            {
                UserId = user.Id,
                UserEmail = user.Email ?? string.Empty
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                return NotFound();
            }

            // Generate reset token and set new password securely
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);

            if (result.Succeeded)
            {
                var currentAdmin = await _userManager.GetUserAsync(User);
                await _auditService.LogAsync(
                    userId: currentAdmin?.Id,
                    action: "AdminPasswordReset",
                    entityName: "User",
                    recordId: user.Id,
                    oldValue: null,
                    newValue: null,
                    result: "Success",
                    details: $"Administrator reset password for user '{user.Email}'.",
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString());

                TempData["SuccessMessage"] = $"Password for '{user.Email}' reset successfully.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }
    }
}
