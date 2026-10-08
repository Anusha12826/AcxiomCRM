using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Models
{
    public class AuditLog
    {
        public long AuditLogId { get; set; }

        [StringLength(450)]
        public string? UserId { get; set; }

        [Required]
        [StringLength(100)]
        public string Action { get; set; } = string.Empty;

        [StringLength(100)]
        public string? EntityName { get; set; }

        [StringLength(100)]
        public string? RecordId { get; set; }

        public string? OldValue { get; set; }

        public string? NewValue { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [StringLength(50)]
        public string? IpAddress { get; set; }

        [StringLength(50)]
        public string? Result { get; set; } = "Success";

        [StringLength(1000)]
        public string? Details { get; set; }

        // Navigation property
        public virtual ApplicationUser? User { get; set; }
    }
}
