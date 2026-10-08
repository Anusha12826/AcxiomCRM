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
    [Route("api/leads")]
    [ApiController]
    [Authorize]
    public class LeadsApiController : ControllerBase
    {
        private readonly ILeadService _leadService;
        private readonly UserManager<ApplicationUser> _userManager;

        public LeadsApiController(
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
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<LeadDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? search = null,
            [FromQuery] string? status = null,
            [FromQuery] string? priority = null,
            [FromQuery] string? assignedTo = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var (userId, role) = await GetUserContextAsync();
            var result = await _leadService.GetLeadsAsync(userId, role, search, status, priority, assignedTo, page, pageSize);

            var dtos = result.Leads.Select(l => new LeadDto
            {
                LeadId = l.LeadId,
                LeadCode = l.LeadCode,
                LeadName = l.LeadName,
                Email = l.Email,
                Phone = l.Phone,
                CompanyName = l.CompanyName,
                Source = l.Source,
                Status = l.Status,
                Priority = l.Priority,
                ExpectedValue = l.ExpectedValue,
                CreatedDate = l.CreatedDate,
                AssignedTo = l.AssignedTo,
                AssignedUserName = l.AssignedUser?.FullName ?? l.AssignedUser?.Email,
                ConvertedCustomerId = l.ConvertedCustomerId,
                ConvertedOpportunityId = l.ConvertedOpportunityId
            });

            return Ok(ApiResponse<IEnumerable<LeadDto>>.Ok(dtos));
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<LeadDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var (userId, role) = await GetUserContextAsync();
            var lead = await _leadService.GetLeadByIdAsync(id, userId, role);
            if (lead == null)
            {
                return NotFound(ApiResponse<LeadDto>.Fail($"Lead with ID {id} not found or unauthorized."));
            }

            var dto = new LeadDto
            {
                LeadId = lead.LeadId,
                LeadCode = lead.LeadCode,
                LeadName = lead.LeadName,
                Email = lead.Email,
                Phone = lead.Phone,
                CompanyName = lead.CompanyName,
                Source = lead.Source,
                Status = lead.Status,
                Priority = lead.Priority,
                ExpectedValue = lead.ExpectedValue,
                CreatedDate = lead.CreatedDate,
                AssignedTo = lead.AssignedTo,
                AssignedUserName = lead.AssignedUser?.FullName ?? lead.AssignedUser?.Email,
                ConvertedCustomerId = lead.ConvertedCustomerId,
                ConvertedOpportunityId = lead.ConvertedOpportunityId
            };

            return Ok(ApiResponse<LeadDto>.Ok(dto));
        }

        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<LeadDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] LeadCreateDto dto)
        {
            var (userId, _) = await GetUserContextAsync();

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<LeadDto>.Fail("Validation failed.", errors));
            }

            var lead = new Lead
            {
                LeadCode = !string.IsNullOrWhiteSpace(dto.LeadCode) ? dto.LeadCode.Trim() : await _leadService.GenerateLeadCodeAsync(),
                LeadName = dto.LeadName.Trim(),
                Email = dto.Email.Trim(),
                Phone = dto.Phone.Trim(),
                CompanyName = dto.CompanyName?.Trim(),
                Source = dto.Source.Trim(),
                Priority = dto.Priority,
                ExpectedValue = dto.ExpectedValue,
                Status = LeadStatuses.New,
                AssignedTo = !string.IsNullOrEmpty(dto.AssignedTo) ? dto.AssignedTo : userId
            };

            var success = await _leadService.CreateLeadAsync(lead, userId);
            if (!success)
            {
                return BadRequest(ApiResponse<LeadDto>.Fail("Failed to create lead."));
            }

            var responseDto = new LeadDto
            {
                LeadId = lead.LeadId,
                LeadCode = lead.LeadCode,
                LeadName = lead.LeadName,
                Email = lead.Email,
                Phone = lead.Phone,
                CompanyName = lead.CompanyName,
                Source = lead.Source,
                Status = lead.Status,
                Priority = lead.Priority,
                ExpectedValue = lead.ExpectedValue,
                CreatedDate = lead.CreatedDate,
                AssignedTo = lead.AssignedTo
            };

            return CreatedAtAction(nameof(GetById), new { id = lead.LeadId }, ApiResponse<LeadDto>.Ok(responseDto, "Lead created successfully."));
        }

        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, [FromBody] LeadUpdateDto dto)
        {
            var (userId, role) = await GetUserContextAsync();

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<string>.Fail("Validation failed.", errors));
            }

            var existing = await _leadService.GetLeadByIdAsync(id, userId, role);
            if (existing == null)
            {
                return NotFound(ApiResponse<string>.Fail($"Lead with ID {id} not found."));
            }

            existing.LeadName = dto.LeadName.Trim();
            existing.Email = dto.Email.Trim();
            existing.Phone = dto.Phone.Trim();
            existing.CompanyName = dto.CompanyName?.Trim();
            existing.Source = dto.Source.Trim();
            existing.Status = dto.Status;
            existing.Priority = dto.Priority;
            existing.ExpectedValue = dto.ExpectedValue;
            existing.AssignedTo = dto.AssignedTo ?? existing.AssignedTo;

            var success = await _leadService.UpdateLeadAsync(existing, userId);
            if (!success)
            {
                return BadRequest(ApiResponse<string>.Fail("Failed to update lead."));
            }

            return Ok(ApiResponse<string>.Ok("Lead updated successfully."));
        }

        [HttpPost("{id}/convert")]
        [ProducesResponseType(typeof(ApiResponse<LeadConvertResultDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Convert(int id, [FromBody] LeadConvertDto dto)
        {
            var (userId, role) = await GetUserContextAsync();
            var existingLead = await _leadService.GetLeadByIdAsync(id, userId, role);
            if (existingLead == null)
            {
                return NotFound(ApiResponse<LeadConvertResultDto>.Fail($"Lead with ID {id} not found or unauthorized."));
            }

            var model = new LeadConvertViewModel
            {
                LeadId = id,
                LeadCode = existingLead.LeadCode,
                CustomerName = !string.IsNullOrWhiteSpace(dto.CustomerName) ? dto.CustomerName.Trim() : existingLead.LeadName,
                Email = !string.IsNullOrWhiteSpace(dto.Email) ? dto.Email.Trim() : existingLead.Email,
                Phone = !string.IsNullOrWhiteSpace(dto.Phone) ? dto.Phone.Trim() : existingLead.Phone,
                CompanyName = !string.IsNullOrWhiteSpace(dto.CompanyName) ? dto.CompanyName.Trim() : existingLead.CompanyName,
                Address = dto.Address,
                City = dto.City,
                State = dto.State,
                CreateOpportunity = dto.CreateOpportunity,
                OpportunityName = !string.IsNullOrWhiteSpace(dto.OpportunityName) ? dto.OpportunityName.Trim() : $"{existingLead.LeadName} - Opportunity",
                OpportunityAmount = dto.OpportunityAmount > 0 ? dto.OpportunityAmount : (existingLead.ExpectedValue > 0 ? existingLead.ExpectedValue : 1000m),
                OpportunityStage = !string.IsNullOrWhiteSpace(dto.OpportunityStage) ? dto.OpportunityStage : OpportunityStages.Qualification,
                Probability = dto.Probability > 0 ? dto.Probability : 30,
                ExpectedCloseDate = dto.ExpectedCloseDate.HasValue && dto.ExpectedCloseDate.Value >= DateTime.Today ? dto.ExpectedCloseDate.Value : DateTime.Today.AddDays(30),
                AssignedTo = dto.AssignedTo ?? existingLead.AssignedTo
            };

            var result = await _leadService.ConvertLeadAsync(model, userId);
            if (!result.Success)
            {
                return BadRequest(ApiResponse<LeadConvertResultDto>.Fail(result.Message));
            }

            var resultDto = new LeadConvertResultDto
            {
                CustomerId = result.CustomerId,
                OpportunityId = result.OpportunityId
            };

            return Ok(ApiResponse<LeadConvertResultDto>.Ok(resultDto, result.Message));
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            var (userId, role) = await GetUserContextAsync();

            if (role != AppRoles.Admin && role != AppRoles.Manager)
            {
                return Forbid();
            }

            var existing = await _leadService.GetLeadByIdAsync(id, userId, role);
            if (existing == null)
            {
                return NotFound(ApiResponse<string>.Fail($"Lead with ID {id} not found."));
            }

            var success = await _leadService.DeleteLeadAsync(id, userId);
            if (!success)
            {
                return BadRequest(ApiResponse<string>.Fail("Failed to delete lead."));
            }

            return Ok(ApiResponse<string>.Ok("Lead deleted successfully."));
        }
    }
}
