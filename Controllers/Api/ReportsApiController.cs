using AcxiomCRM.DTOs;
using AcxiomCRM.Models;
using AcxiomCRM.Models.Constants;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers.Api
{
    [Route("api/reports")]
    [ApiController]
    [Authorize]
    public class ReportsApiController : ControllerBase
    {
        private readonly IReportService _reportService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReportsApiController(
            IReportService reportService,
            UserManager<ApplicationUser> userManager)
        {
            _reportService = reportService;
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

        /// <summary>
        /// Retrieves pipeline stage and owner distribution report data.
        /// </summary>
        [HttpGet("pipeline")]
        [ProducesResponseType(typeof(ApiResponse<PipelineReportViewModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPipelineReport(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] string? stage = null,
            [FromQuery] string? assignedTo = null)
        {
            var (userId, role) = await GetUserContextAsync();

            var filter = new ReportFilterViewModel
            {
                FromDate = fromDate,
                ToDate = toDate,
                Stage = stage,
                AssignedTo = assignedTo,
                ReportType = "Pipeline"
            };

            var result = await _reportService.GetPipelineReportAsync(userId, role, filter);
            return Ok(ApiResponse<PipelineReportViewModel>.Ok(result, "Pipeline report data retrieved successfully."));
        }
    }
}
