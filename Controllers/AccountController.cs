using AcxiomCRM.Models;
using AcxiomCRM.Models.Constants;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditService _auditService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            IAuditService auditService,
            ILogger<AccountController> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _auditService = auditService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                await _auditService.LogAsync(
                    userId: null,
                    action: "FailedLogin",
                    entityName: "User",
                    recordId: model.Email,
                    oldValue: null,
                    newValue: null,
                    result: "Failed",
                    details: $"Login failed: User '{model.Email}' not found.",
                    ipAddress: clientIp);

                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return View(model);
            }

            // Verify active status
            if (!user.IsActive)
            {
                await _auditService.LogAsync(
                    userId: user.Id,
                    action: "FailedLogin",
                    entityName: "User",
                    recordId: user.Id,
                    oldValue: null,
                    newValue: null,
                    result: "Blocked",
                    details: $"Login blocked: User '{model.Email}' account is deactivated.",
                    ipAddress: clientIp);

                ModelState.AddModelError(string.Empty, "Your account has been deactivated. Please contact an administrator.");
                return View(model);
            }

            // Attempt login with lockout tracking
            var result = await _signInManager.PasswordSignInAsync(
                user.UserName!,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: true);

            if (result.Succeeded)
            {
                await _auditService.LogAsync(
                    userId: user.Id,
                    action: "Login",
                    entityName: "User",
                    recordId: user.Id,
                    oldValue: null,
                    newValue: null,
                    result: "Success",
                    details: $"User '{user.Email}' logged in successfully.",
                    ipAddress: clientIp);

                _logger.LogInformation("User {Email} logged in.", model.Email);

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction("Index", "Home");
            }

            if (result.IsLockedOut)
            {
                await _auditService.LogAsync(
                    userId: user.Id,
                    action: "AccountLockedOut",
                    entityName: "User",
                    recordId: user.Id,
                    oldValue: null,
                    newValue: null,
                    result: "LockedOut",
                    details: $"Account '{user.Email}' locked out due to multiple failed login attempts.",
                    ipAddress: clientIp);

                _logger.LogWarning("User {Email} account locked out.", model.Email);
                return RedirectToAction(nameof(Lockout));
            }

            // Invalid password attempt
            await _auditService.LogAsync(
                userId: user.Id,
                action: "FailedLogin",
                entityName: "User",
                recordId: user.Id,
                oldValue: null,
                newValue: null,
                result: "Failed",
                details: $"Failed login attempt for '{user.Email}'. Failed access count: {user.AccessFailedCount + 1}",
                ipAddress: clientIp);

            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var user = await _userManager.GetUserAsync(User);
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();

            if (user != null)
            {
                await _auditService.LogAsync(
                    userId: user.Id,
                    action: "Logout",
                    entityName: "User",
                    recordId: user.Id,
                    oldValue: null,
                    newValue: null,
                    result: "Success",
                    details: $"User '{user.Email}' logged out.",
                    ipAddress: clientIp);
            }

            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError(nameof(model.Email), "An account with this email address already exists.");
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                EmailConfirmed = true,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                // Assign role (default to SalesExecutive)
                var assignedRole = AppRoles.AllRoles.Contains(model.Role) ? model.Role : AppRoles.SalesExecutive;
                await _userManager.AddToRoleAsync(user, assignedRole);

                await _auditService.LogAsync(
                    userId: user.Id,
                    action: "UserRegistered",
                    entityName: "User",
                    recordId: user.Id,
                    oldValue: null,
                    newValue: $"Role: {assignedRole}",
                    result: "Success",
                    details: $"User '{user.Email}' self-registered with role '{assignedRole}'.",
                    ipAddress: clientIp);

                TempData["SuccessMessage"] = "Registration successful! You may now log in.";
                return RedirectToAction(nameof(Login));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [HttpGet]
        [Authorize]
        public IActionResult ChangePassword()
        {
            return View(new ChangePasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction(nameof(Login));
            }

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);

                await _auditService.LogAsync(
                    userId: user.Id,
                    action: "PasswordChanged",
                    entityName: "User",
                    recordId: user.Id,
                    oldValue: null,
                    newValue: null,
                    result: "Success",
                    details: $"User '{user.Email}' changed their password.",
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString());

                TempData["SuccessMessage"] = "Your password has been changed successfully.";
                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Lockout()
        {
            return View();
        }
    }
}
