using AcxiomCRM.DTOs;
using AcxiomCRM.Models;
using AcxiomCRM.Models.Constants;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers.Api
{
    [Route("api/opportunities")]
    [ApiController]
    [Authorize]
    public class OpportunitiesApiController : ControllerBase
    {
        private readonly IOpportunityService _opportunityService;
        private readonly UserManager<ApplicationUser> _userManager;

        public OpportunitiesApiController(
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

        /// <summary>
        /// Retrieves a paginated list of opportunities scoped to user permissions.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<OpportunityDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? search = null,
            [FromQuery] string? stage = null,
            [FromQuery] string? status = null,
            [FromQuery] string? assignedTo = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var (userId, role) = await GetUserContextAsync();
            var result = await _opportunityService.GetOpportunitiesAsync(userId, role, search, stage, status, assignedTo, page, pageSize);

            var dtos = result.Opportunities.Select(o => new OpportunityDto
            {
                OpportunityId = o.OpportunityId,
                OpportunityName = o.OpportunityName,
                CustomerId = o.CustomerId,
                CustomerName = o.Customer?.CustomerName,
                LeadId = o.LeadId,
                LeadName = o.Lead?.LeadName,
                Amount = o.Amount,
                Stage = o.Stage,
                Probability = o.Probability,
                WeightedPipeline = (o.Amount * o.Probability) / 100m,
                ExpectedCloseDate = o.ExpectedCloseDate,
                Status = o.Status,
                CreatedDate = o.CreatedDate,
                AssignedTo = o.AssignedTo,
                AssignedUserName = o.AssignedUser?.FullName ?? o.AssignedUser?.Email
            });

            return Ok(ApiResponse<IEnumerable<OpportunityDto>>.Ok(dtos));
        }

        /// <summary>
        /// Retrieves a single opportunity by ID.
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<OpportunityDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var (userId, role) = await GetUserContextAsync();
            var opp = await _opportunityService.GetOpportunityByIdAsync(id, userId, role);
            if (opp == null)
            {
                return NotFound(ApiResponse<OpportunityDto>.Fail($"Opportunity with ID {id} not found or unauthorized."));
            }

            var dto = new OpportunityDto
            {
                OpportunityId = opp.OpportunityId,
                OpportunityName = opp.OpportunityName,
                CustomerId = opp.CustomerId,
                CustomerName = opp.Customer?.CustomerName,
                LeadId = opp.LeadId,
                LeadName = opp.Lead?.LeadName,
                Amount = opp.Amount,
                Stage = opp.Stage,
                Probability = opp.Probability,
                WeightedPipeline = (opp.Amount * opp.Probability) / 100m,
                ExpectedCloseDate = opp.ExpectedCloseDate,
                Status = opp.Status,
                CreatedDate = opp.CreatedDate,
                AssignedTo = opp.AssignedTo,
                AssignedUserName = opp.AssignedUser?.FullName ?? opp.AssignedUser?.Email
            };

            return Ok(ApiResponse<OpportunityDto>.Ok(dto));
        }

        /// <summary>
        /// Creates a new opportunity with strict business rules.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<OpportunityDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] OpportunityCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<OpportunityDto>.Fail("Validation failed.", errors));
            }

            if (dto.Amount <= 0)
            {
                return BadRequest(ApiResponse<OpportunityDto>.Fail("Opportunity Amount must be greater than 0."));
            }

            if (dto.Probability < 0 || dto.Probability > 100)
            {
                return BadRequest(ApiResponse<OpportunityDto>.Fail("Probability must be between 0 and 100."));
            }

            if (dto.ExpectedCloseDate.Date < DateTime.UtcNow.Date)
            {
                return BadRequest(ApiResponse<OpportunityDto>.Fail("Expected Close Date cannot be in the past."));
            }

            var (userId, role) = await GetUserContextAsync();

            var status = OpportunityStatuses.Open;
            var probability = dto.Probability;

            if (dto.Stage == OpportunityStages.Won)
            {
                status = OpportunityStatuses.Won;
                probability = 100;
            }
            else if (dto.Stage == OpportunityStages.Lost)
            {
                status = OpportunityStatuses.Lost;
                probability = 0;
            }

            var assignedTo = (role == AppRoles.SalesExecutive) ? userId : dto.AssignedTo ?? userId;

            var opportunity = new Opportunity
            {
                OpportunityName = dto.OpportunityName.Trim(),
                CustomerId = dto.CustomerId,
                LeadId = dto.LeadId,
                Amount = dto.Amount,
                Stage = dto.Stage,
                Probability = probability,
                ExpectedCloseDate = dto.ExpectedCloseDate,
                Status = status,
                AssignedTo = assignedTo,
                CreatedDate = DateTime.UtcNow
            };

            var success = await _opportunityService.CreateOpportunityAsync(opportunity, userId);
            if (!success)
            {
                return BadRequest(ApiResponse<OpportunityDto>.Fail("Failed to create opportunity. Check related Customer or Lead existence."));
            }

            var createdDto = new OpportunityDto
            {
                OpportunityId = opportunity.OpportunityId,
                OpportunityName = opportunity.OpportunityName,
                CustomerId = opportunity.CustomerId,
                LeadId = opportunity.LeadId,
                Amount = opportunity.Amount,
                Stage = opportunity.Stage,
                Probability = opportunity.Probability,
                WeightedPipeline = (opportunity.Amount * opportunity.Probability) / 100m,
                ExpectedCloseDate = opportunity.ExpectedCloseDate,
                Status = opportunity.Status,
                CreatedDate = opportunity.CreatedDate,
                AssignedTo = opportunity.AssignedTo
            };

            return CreatedAtAction(nameof(GetById), new { id = opportunity.OpportunityId }, ApiResponse<OpportunityDto>.Ok(createdDto, "Opportunity created successfully."));
        }

        /// <summary>
        /// Updates an existing opportunity.
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResponse<OpportunityDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, [FromBody] OpportunityUpdateDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<OpportunityDto>.Fail("Validation failed.", errors));
            }

            if (dto.Amount <= 0)
            {
                return BadRequest(ApiResponse<OpportunityDto>.Fail("Opportunity Amount must be greater than 0."));
            }

            if (dto.Probability < 0 || dto.Probability > 100)
            {
                return BadRequest(ApiResponse<OpportunityDto>.Fail("Probability must be between 0 and 100."));
            }

            var (userId, role) = await GetUserContextAsync();
            var existing = await _opportunityService.GetOpportunityByIdAsync(id, userId, role);
            if (existing == null)
            {
                return NotFound(ApiResponse<OpportunityDto>.Fail($"Opportunity with ID {id} not found or unauthorized."));
            }

            if (dto.Stage != OpportunityStages.Won && dto.Stage != OpportunityStages.Lost && dto.ExpectedCloseDate.Date < DateTime.UtcNow.Date)
            {
                return BadRequest(ApiResponse<OpportunityDto>.Fail("Expected Close Date cannot be in the past."));
            }

            existing.OpportunityName = dto.OpportunityName.Trim();
            existing.CustomerId = dto.CustomerId;
            existing.LeadId = dto.LeadId;
            existing.Amount = dto.Amount;
            existing.Stage = dto.Stage;
            existing.Probability = dto.Probability;
            existing.ExpectedCloseDate = dto.ExpectedCloseDate;
            existing.Status = dto.Status;

            if (dto.Stage == OpportunityStages.Won)
            {
                existing.Status = OpportunityStatuses.Won;
                existing.Probability = 100;
            }
            else if (dto.Stage == OpportunityStages.Lost)
            {
                existing.Status = OpportunityStatuses.Lost;
                existing.Probability = 0;
            }

            if (role != AppRoles.SalesExecutive && !string.IsNullOrEmpty(dto.AssignedTo))
            {
                existing.AssignedTo = dto.AssignedTo;
            }

            var success = await _opportunityService.UpdateOpportunityAsync(existing, userId);
            if (!success)
            {
                return BadRequest(ApiResponse<OpportunityDto>.Fail("Failed to update opportunity."));
            }

            var updatedDto = new OpportunityDto
            {
                OpportunityId = existing.OpportunityId,
                OpportunityName = existing.OpportunityName,
                CustomerId = existing.CustomerId,
                LeadId = existing.LeadId,
                Amount = existing.Amount,
                Stage = existing.Stage,
                Probability = existing.Probability,
                WeightedPipeline = (existing.Amount * existing.Probability) / 100m,
                ExpectedCloseDate = existing.ExpectedCloseDate,
                Status = existing.Status,
                CreatedDate = existing.CreatedDate,
                AssignedTo = existing.AssignedTo
            };

            return Ok(ApiResponse<OpportunityDto>.Ok(updatedDto, "Opportunity updated successfully."));
        }

        /// <summary>
        /// Updates the pipeline stage for an opportunity.
        /// </summary>
        [HttpPatch("{id}/stage")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateStage(int id, [FromBody] StageUpdateDto dto)
        {
            var validStages = new[] { OpportunityStages.Qualification, OpportunityStages.Proposal, OpportunityStages.Negotiation, OpportunityStages.Won, OpportunityStages.Lost };
            if (string.IsNullOrWhiteSpace(dto.Stage) || !validStages.Contains(dto.Stage))
            {
                return BadRequest(ApiResponse<bool>.Fail($"Invalid stage '{dto.Stage}'. Valid stages: {string.Join(", ", validStages)}"));
            }

            var (userId, role) = await GetUserContextAsync();
            var opp = await _opportunityService.GetOpportunityByIdAsync(id, userId, role);
            if (opp == null)
            {
                return NotFound(ApiResponse<bool>.Fail($"Opportunity with ID {id} not found or unauthorized."));
            }

            var success = await _opportunityService.UpdateStageAsync(id, dto.Stage, userId);
            if (!success)
            {
                return BadRequest(ApiResponse<bool>.Fail("Failed to update opportunity stage."));
            }

            return Ok(ApiResponse<bool>.Ok(true, $"Opportunity stage successfully updated to '{dto.Stage}'."));
        }

        /// <summary>
        /// Deactivates or removes an opportunity (Manager or Admin).
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Manager}")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            var (userId, role) = await GetUserContextAsync();
            var opp = await _opportunityService.GetOpportunityByIdAsync(id, userId, role);
            if (opp == null)
            {
                return NotFound(ApiResponse<bool>.Fail($"Opportunity with ID {id} not found or unauthorized."));
            }

            var success = await _opportunityService.DeleteOpportunityAsync(id, userId);
            if (!success)
            {
                return BadRequest(ApiResponse<bool>.Fail("Failed to delete opportunity."));
            }

            return Ok(ApiResponse<bool>.Ok(true, "Opportunity successfully deleted."));
        }
    }

    public class StageUpdateDto
    {
        public string Stage { get; set; } = string.Empty;
    }
}
