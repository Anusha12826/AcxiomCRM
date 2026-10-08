using AcxiomCRM.Models.Constants;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers
{
    [Authorize(Roles = AppRoles.Admin)]
    public class AuditController : Controller
    {
        private readonly IAuditService _auditService;

        public AuditController(IAuditService auditService)
        {
            _auditService = auditService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string? userId = null,
            string? entityName = null,
            string? action = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int page = 1)
        {
            var model = await _auditService.GetPagedAuditLogsAsync(userId, entityName, action, fromDate, toDate, page, 15);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Details(long id)
        {
            var log = await _auditService.GetAuditLogByIdAsync(id);
            if (log == null)
            {
                TempData["ErrorMessage"] = "Audit log record not found.";
                return RedirectToAction(nameof(Index));
            }

            return View(log);
        }
    }
}
