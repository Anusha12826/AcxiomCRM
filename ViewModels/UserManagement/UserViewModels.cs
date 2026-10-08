using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Models.Constants;

namespace AcxiomCRM.ViewModels.UserManagement
{
    public class UserListViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool IsLockedOut { get; set; }
        public int AccessFailedCount { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class CreateUserViewModel
    {
        [Required(ErrorMessage = "Full Name is required.")]
        [StringLength(150, ErrorMessage = "Full Name cannot exceed 150 characters.")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, ErrorMessage = "Password must be at least {2} characters long.", MinimumLength = 8)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirm Password is required.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Role selection is required.")]
        [Display(Name = "Primary Role")]
        public string Role { get; set; } = AppRoles.SalesExecutive;

        [Display(Name = "Account Status")]
        public bool IsActive { get; set; } = true;
    }

    public class EditUserViewModel
    {
        [Required]
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = "Full Name is required.")]
        [StringLength(150, ErrorMessage = "Full Name cannot exceed 150 characters.")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Role selection is required.")]
        [Display(Name = "Primary Role")]
        public string Role { get; set; } = string.Empty;

        [Display(Name = "Account Active")]
        public bool IsActive { get; set; } = true;
    }
}
