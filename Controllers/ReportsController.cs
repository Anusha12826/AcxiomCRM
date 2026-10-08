using AcxiomCRM.Models;
using AcxiomCRM.Models.Constants;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace AcxiomCRM.Controllers
{
    [Authorize]
    [Route("Reports")]
    public class ReportsController : Controller
    {
        private readonly IReportService _reportService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReportsController(
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

        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index()
        {
            var (userId, role) = await GetUserContextAsync();
            ViewBag.UserRole = role;
            return View();
        }

        [HttpGet("Customers")]
        public async Task<IActionResult> Customers([FromQuery] ReportFilterViewModel filter)
        {
            var (userId, role) = await GetUserContextAsync();
            filter.ReportType = "Customers";
            var vm = await _reportService.GetCustomerReportAsync(userId, role, filter);
            return View(vm);
        }

        [HttpGet("Leads")]
        public async Task<IActionResult> Leads([FromQuery] ReportFilterViewModel filter)
        {
            var (userId, role) = await GetUserContextAsync();
            filter.ReportType = "Leads";
            var vm = await _reportService.GetLeadReportAsync(userId, role, filter);
            return View(vm);
        }

        [HttpGet("FollowUps")]
        public async Task<IActionResult> FollowUps([FromQuery] ReportFilterViewModel filter)
        {
            var (userId, role) = await GetUserContextAsync();
            filter.ReportType = "FollowUps";
            var vm = await _reportService.GetFollowUpReportAsync(userId, role, filter);
            return View(vm);
        }

        [HttpGet("Opportunities")]
        public async Task<IActionResult> Opportunities([FromQuery] ReportFilterViewModel filter)
        {
            var (userId, role) = await GetUserContextAsync();
            filter.ReportType = "Opportunities";
            var vm = await _reportService.GetOpportunityReportAsync(userId, role, filter);
            return View(vm);
        }

        [HttpGet("Pipeline")]
        public async Task<IActionResult> Pipeline([FromQuery] ReportFilterViewModel filter)
        {
            var (userId, role) = await GetUserContextAsync();
            filter.ReportType = "Pipeline";
            var vm = await _reportService.GetPipelineReportAsync(userId, role, filter);
            ViewBag.UserRole = role;
            return View(vm);
        }

        [HttpGet("Conversion")]
        public async Task<IActionResult> Conversion([FromQuery] ReportFilterViewModel filter)
        {
            var (userId, role) = await GetUserContextAsync();
            filter.ReportType = "Conversion";
            var vm = await _reportService.GetConversionReportAsync(userId, role, filter);
            ViewBag.UserRole = role;
            return View(vm);
        }

        [HttpGet("UserActivity")]
        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Manager}")]
        public async Task<IActionResult> UserActivity([FromQuery] ReportFilterViewModel filter)
        {
            var (userId, role) = await GetUserContextAsync();
            filter.ReportType = "UserActivity";
            var vm = await _reportService.GetUserActivityReportAsync(userId, role, filter);
            return View(vm);
        }

        [HttpGet("Audit")]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> Audit([FromQuery] ReportFilterViewModel filter)
        {
            var (userId, role) = await GetUserContextAsync();
            filter.ReportType = "Audit";
            var vm = await _reportService.GetAuditReportAsync(userId, role, filter);
            return View(vm);
        }

        [HttpGet("ExportCsv")]
        public async Task<IActionResult> ExportCsv([FromQuery] string reportType, [FromQuery] ReportFilterViewModel filter)
        {
            var (userId, role) = await GetUserContextAsync();
            var sb = new StringBuilder();
            var fileName = $"{reportType}_Report_{DateTime.UtcNow:yyyyMMdd_HHmm}.csv";

            switch (reportType.ToLowerInvariant())
            {
                case "customers":
                    var custs = await _reportService.GetCustomerReportAsync(userId, role, filter);
                    sb.AppendLine("Code,Name,Email,Phone,Company,City,Status,Owner,CreatedDate");
                    foreach (var c in custs.Items)
                    {
                        sb.AppendLine($"\"{c.CustomerCode}\",\"{c.CustomerName}\",\"{c.Email}\",\"{c.Phone}\",\"{c.CompanyName}\",\"{c.City}\",\"{c.Status}\",\"{c.OwnerName}\",\"{c.CreatedDate:yyyy-MM-dd HH:mm}\"");
                    }
                    break;

                case "leads":
                    var leads = await _reportService.GetLeadReportAsync(userId, role, filter);
                    sb.AppendLine("Code,Name,Email,Phone,Company,Source,Status,Priority,ExpectedValue,Owner,CreatedDate,Converted");
                    foreach (var l in leads.Items)
                    {
                        sb.AppendLine($"\"{l.LeadCode}\",\"{l.LeadName}\",\"{l.Email}\",\"{l.Phone}\",\"{l.CompanyName}\",\"{l.Source}\",\"{l.Status}\",\"{l.Priority}\",{l.ExpectedValue},\"{l.OwnerName}\",\"{l.CreatedDate:yyyy-MM-dd HH:mm}\",{l.IsConverted}");
                    }
                    break;

                case "followups":
                    var fllws = await _reportService.GetFollowUpReportAsync(userId, role, filter);
                    sb.AppendLine("ID,Entity,Type,Date,Status,Overdue,Owner,Remarks");
                    foreach (var f in fllws.Items)
                    {
                        sb.AppendLine($"{f.FollowUpId},\"{f.RelatedEntityName}\",\"{f.FollowUpType}\",\"{f.FollowUpDate:yyyy-MM-dd HH:mm}\",\"{f.Status}\",{f.IsOverdue},\"{f.OwnerName}\",\"{f.Remarks?.Replace("\"", "\"\"")}\"");
                    }
                    break;

                case "opportunities":
                case "pipeline":
                    var opps = await _reportService.GetOpportunityReportAsync(userId, role, filter);
                    sb.AppendLine("ID,OpportunityName,Customer/Lead,Stage,Amount,Probability,WeightedValue,CloseDate,Status,Owner,CreatedDate");
                    foreach (var o in opps.Items)
                    {
                        sb.AppendLine($"{o.OpportunityId},\"{o.OpportunityName}\",\"{o.CustomerOrLeadName}\",\"{o.Stage}\",{o.Amount},{o.Probability},{o.WeightedAmount},\"{o.ExpectedCloseDate:yyyy-MM-dd}\",\"{o.Status}\",\"{o.OwnerName}\",\"{o.CreatedDate:yyyy-MM-dd}\"");
                    }
                    break;

                default:
                    return BadRequest("Unsupported export report type.");
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv", fileName);
        }
    }
}
