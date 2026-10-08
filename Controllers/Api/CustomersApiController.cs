using AcxiomCRM.DTOs;
using AcxiomCRM.Models;
using AcxiomCRM.Models.Constants;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers.Api
{
    [Route("api/customers")]
    [ApiController]
    [Authorize]
    public class CustomersApiController : ControllerBase
    {
        private readonly ICustomerService _customerService;
        private readonly UserManager<ApplicationUser> _userManager;

        public CustomersApiController(
            ICustomerService customerService,
            UserManager<ApplicationUser> userManager)
        {
            _customerService = customerService;
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
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<CustomerDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? search = null,
            [FromQuery] string? status = null,
            [FromQuery] string? assignedTo = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var (userId, role) = await GetUserContextAsync();
            var result = await _customerService.GetCustomersAsync(userId, role, search, status, assignedTo, page, pageSize);

            var dtos = result.Customers.Select(c => new CustomerDto
            {
                CustomerId = c.CustomerId,
                CustomerCode = c.CustomerCode,
                CustomerName = c.CustomerName,
                Email = c.Email,
                Phone = c.Phone,
                CompanyName = c.CompanyName,
                Address = c.Address,
                City = c.City,
                State = c.State,
                Status = c.Status,
                CreatedDate = c.CreatedDate,
                AssignedTo = c.AssignedTo,
                AssignedUserName = c.AssignedUser?.FullName ?? c.AssignedUser?.Email
            });

            return Ok(ApiResponse<IEnumerable<CustomerDto>>.Ok(dtos));
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<CustomerDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var (userId, role) = await GetUserContextAsync();
            var customer = await _customerService.GetCustomerByIdAsync(id, userId, role);
            if (customer == null)
            {
                return NotFound(ApiResponse<CustomerDto>.Fail($"Customer with ID {id} not found or unauthorized."));
            }

            var dto = new CustomerDto
            {
                CustomerId = customer.CustomerId,
                CustomerCode = customer.CustomerCode,
                CustomerName = customer.CustomerName,
                Email = customer.Email,
                Phone = customer.Phone,
                CompanyName = customer.CompanyName,
                Address = customer.Address,
                City = customer.City,
                State = customer.State,
                Status = customer.Status,
                CreatedDate = customer.CreatedDate,
                AssignedTo = customer.AssignedTo,
                AssignedUserName = customer.AssignedUser?.FullName ?? customer.AssignedUser?.Email
            };

            return Ok(ApiResponse<CustomerDto>.Ok(dto));
        }

        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<CustomerDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CustomerCreateDto dto)
        {
            var (userId, _) = await GetUserContextAsync();

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<CustomerDto>.Fail("Validation failed.", errors));
            }

            if (!await _customerService.IsEmailUniqueAsync(dto.Email))
            {
                return BadRequest(ApiResponse<CustomerDto>.Fail("A customer with this email address already exists."));
            }

            if (!await _customerService.IsPhoneUniqueAsync(dto.Phone))
            {
                return BadRequest(ApiResponse<CustomerDto>.Fail("A customer with this phone number already exists."));
            }

            var customer = new Customer
            {
                CustomerCode = !string.IsNullOrWhiteSpace(dto.CustomerCode) ? dto.CustomerCode.Trim() : await _customerService.GenerateCustomerCodeAsync(),
                CustomerName = dto.CustomerName.Trim(),
                Email = dto.Email.Trim(),
                Phone = dto.Phone.Trim(),
                CompanyName = dto.CompanyName?.Trim(),
                Address = dto.Address?.Trim(),
                City = dto.City?.Trim(),
                State = dto.State?.Trim(),
                Status = CustomerStatuses.Active,
                AssignedTo = !string.IsNullOrEmpty(dto.AssignedTo) ? dto.AssignedTo : userId
            };

            var success = await _customerService.CreateCustomerAsync(customer, userId);
            if (!success)
            {
                return BadRequest(ApiResponse<CustomerDto>.Fail("Failed to create customer."));
            }

            var responseDto = new CustomerDto
            {
                CustomerId = customer.CustomerId,
                CustomerCode = customer.CustomerCode,
                CustomerName = customer.CustomerName,
                Email = customer.Email,
                Phone = customer.Phone,
                CompanyName = customer.CompanyName,
                Address = customer.Address,
                City = customer.City,
                State = customer.State,
                Status = customer.Status,
                CreatedDate = customer.CreatedDate,
                AssignedTo = customer.AssignedTo
            };

            return CreatedAtAction(nameof(GetById), new { id = customer.CustomerId }, ApiResponse<CustomerDto>.Ok(responseDto, "Customer created successfully."));
        }

        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, [FromBody] CustomerUpdateDto dto)
        {
            var (userId, role) = await GetUserContextAsync();

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<string>.Fail("Validation failed.", errors));
            }

            var existing = await _customerService.GetCustomerByIdAsync(id, userId, role);
            if (existing == null)
            {
                return NotFound(ApiResponse<string>.Fail($"Customer with ID {id} not found."));
            }

            if (!await _customerService.IsEmailUniqueAsync(dto.Email, id))
            {
                return BadRequest(ApiResponse<string>.Fail("A customer with this email address already exists."));
            }

            if (!await _customerService.IsPhoneUniqueAsync(dto.Phone, id))
            {
                return BadRequest(ApiResponse<string>.Fail("A customer with this phone number already exists."));
            }

            existing.CustomerName = dto.CustomerName.Trim();
            existing.Email = dto.Email.Trim();
            existing.Phone = dto.Phone.Trim();
            existing.CompanyName = dto.CompanyName?.Trim();
            existing.Address = dto.Address?.Trim();
            existing.City = dto.City?.Trim();
            existing.State = dto.State?.Trim();
            existing.Status = dto.Status;
            existing.AssignedTo = dto.AssignedTo ?? existing.AssignedTo;

            var success = await _customerService.UpdateCustomerAsync(existing, userId);
            if (!success)
            {
                return BadRequest(ApiResponse<string>.Fail("Failed to update customer."));
            }

            return Ok(ApiResponse<string>.Ok("Customer updated successfully."));
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

            var existing = await _customerService.GetCustomerByIdAsync(id, userId, role);
            if (existing == null)
            {
                return NotFound(ApiResponse<string>.Fail($"Customer with ID {id} not found."));
            }

            var success = await _customerService.DeleteCustomerAsync(id, userId);
            if (!success)
            {
                return BadRequest(ApiResponse<string>.Fail("Failed to delete customer record."));
            }

            return Ok(ApiResponse<string>.Ok("Customer deleted successfully."));
        }
    }
}
