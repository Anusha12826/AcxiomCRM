using Microsoft.AspNetCore.Identity;

namespace AcxiomCRM.Models
{
    public class ApplicationRole : IdentityRole
    {
        public string? Description { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public ApplicationRole() : base() { }

        public ApplicationRole(string roleName, string? description = null) : base(roleName)
        {
            Description = description;
            CreatedDate = DateTime.UtcNow;
        }
    }
}
