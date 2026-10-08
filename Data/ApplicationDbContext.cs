using AcxiomCRM.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<Lead> Leads => Set<Lead>();
        public DbSet<Opportunity> Opportunities => Set<Opportunity>();
        public DbSet<FollowUp> FollowUps => Set<FollowUp>();
        public DbSet<Activity> Activities => Set<Activity>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Customer Configuration
            builder.Entity<Customer>(entity =>
            {
                entity.HasKey(c => c.CustomerId);
                entity.HasIndex(c => c.CustomerCode).IsUnique();
                entity.HasIndex(c => c.Email).IsUnique();
                entity.HasIndex(c => c.Phone).IsUnique();
                entity.HasIndex(c => c.CustomerName);
                entity.HasIndex(c => c.Status);

                entity.Property(c => c.CustomerCode).HasMaxLength(50).IsRequired();
                entity.Property(c => c.CustomerName).HasMaxLength(150).IsRequired();
                entity.Property(c => c.Email).HasMaxLength(150).IsRequired();
                entity.Property(c => c.Phone).HasMaxLength(20).IsRequired();
                entity.Property(c => c.CompanyName).HasMaxLength(150);
                entity.Property(c => c.Address).HasMaxLength(250);
                entity.Property(c => c.City).HasMaxLength(100);
                entity.Property(c => c.State).HasMaxLength(100);
                entity.Property(c => c.Status).HasMaxLength(50).IsRequired();

                entity.HasOne(c => c.Creator)
                    .WithMany(u => u.CreatedCustomers)
                    .HasForeignKey(c => c.CreatedBy)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(c => c.AssignedUser)
                    .WithMany(u => u.AssignedCustomers)
                    .HasForeignKey(c => c.AssignedTo)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Lead Configuration
            builder.Entity<Lead>(entity =>
            {
                entity.HasKey(l => l.LeadId);
                entity.HasIndex(l => l.LeadCode).IsUnique();
                entity.HasIndex(l => l.LeadName);
                entity.HasIndex(l => l.Status);
                entity.HasIndex(l => l.Source);

                entity.Property(l => l.LeadCode).HasMaxLength(50).IsRequired();
                entity.Property(l => l.LeadName).HasMaxLength(150).IsRequired();
                entity.Property(l => l.Email).HasMaxLength(150).IsRequired();
                entity.Property(l => l.Phone).HasMaxLength(20).IsRequired();
                entity.Property(l => l.CompanyName).HasMaxLength(150);
                entity.Property(l => l.Source).HasMaxLength(100).IsRequired();
                entity.Property(l => l.Status).HasMaxLength(50).IsRequired();
                entity.Property(l => l.Priority).HasMaxLength(50);
                entity.Property(l => l.ExpectedValue).HasPrecision(18, 2);

                entity.HasOne(l => l.AssignedUser)
                    .WithMany(u => u.AssignedLeads)
                    .HasForeignKey(l => l.AssignedTo)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(l => l.ConvertedCustomer)
                    .WithMany()
                    .HasForeignKey(l => l.ConvertedCustomerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(l => l.ConvertedOpportunity)
                    .WithMany()
                    .HasForeignKey(l => l.ConvertedOpportunityId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Opportunity Configuration
            builder.Entity<Opportunity>(entity =>
            {
                entity.HasKey(o => o.OpportunityId);
                entity.HasIndex(o => o.OpportunityName);
                entity.HasIndex(o => o.Stage);
                entity.HasIndex(o => o.Status);

                entity.Property(o => o.OpportunityName).HasMaxLength(150).IsRequired();
                entity.Property(o => o.Stage).HasMaxLength(50).IsRequired();
                entity.Property(o => o.Status).HasMaxLength(50).IsRequired();
                entity.Property(o => o.Amount).HasPrecision(18, 2);

                entity.HasOne(o => o.Customer)
                    .WithMany(c => c.Opportunities)
                    .HasForeignKey(o => o.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(o => o.Lead)
                    .WithMany(l => l.Opportunities)
                    .HasForeignKey(o => o.LeadId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(o => o.AssignedUser)
                    .WithMany(u => u.AssignedOpportunities)
                    .HasForeignKey(o => o.AssignedTo)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // FollowUp Configuration
            builder.Entity<FollowUp>(entity =>
            {
                entity.HasKey(f => f.FollowUpId);
                entity.HasIndex(f => f.FollowUpDate);
                entity.HasIndex(f => f.Status);

                entity.Property(f => f.FollowUpType).HasMaxLength(50).IsRequired();
                entity.Property(f => f.Remarks).HasMaxLength(1000);
                entity.Property(f => f.Status).HasMaxLength(50).IsRequired();

                entity.HasOne(f => f.Customer)
                    .WithMany(c => c.FollowUps)
                    .HasForeignKey(f => f.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(f => f.Lead)
                    .WithMany(l => l.FollowUps)
                    .HasForeignKey(f => f.LeadId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(f => f.Opportunity)
                    .WithMany(o => o.FollowUps)
                    .HasForeignKey(f => f.OpportunityId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(f => f.AssignedUser)
                    .WithMany(u => u.AssignedFollowUps)
                    .HasForeignKey(f => f.AssignedTo)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Activity Configuration
            builder.Entity<Activity>(entity =>
            {
                entity.HasKey(a => a.ActivityId);
                entity.HasIndex(a => a.ActivityDate);
                entity.HasIndex(a => a.ActivityType);
                entity.HasIndex(a => a.Status);

                entity.Property(a => a.ActivityType).HasMaxLength(50).IsRequired();
                entity.Property(a => a.Subject).HasMaxLength(200).IsRequired();
                entity.Property(a => a.Description).HasMaxLength(2000);
                entity.Property(a => a.Status).HasMaxLength(50).IsRequired();

                entity.HasOne(a => a.Customer)
                    .WithMany(c => c.Activities)
                    .HasForeignKey(a => a.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(a => a.Lead)
                    .WithMany(l => l.Activities)
                    .HasForeignKey(a => a.LeadId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(a => a.AssignedUser)
                    .WithMany(u => u.AssignedActivities)
                    .HasForeignKey(a => a.AssignedTo)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // AuditLog Configuration
            builder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(a => a.AuditLogId);
                entity.HasIndex(a => a.CreatedDate);
                entity.HasIndex(a => a.Action);
                entity.HasIndex(a => a.EntityName);
                entity.HasIndex(a => a.UserId);

                entity.Property(a => a.Action).HasMaxLength(100).IsRequired();
                entity.Property(a => a.EntityName).HasMaxLength(100);
                entity.Property(a => a.RecordId).HasMaxLength(100);
                entity.Property(a => a.IpAddress).HasMaxLength(50);
                entity.Property(a => a.Result).HasMaxLength(50);
                entity.Property(a => a.Details).HasMaxLength(1000);

                entity.HasOne(a => a.User)
                    .WithMany(u => u.AuditLogs)
                    .HasForeignKey(a => a.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
