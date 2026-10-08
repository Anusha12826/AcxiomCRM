using AcxiomCRM.Models;
using AcxiomCRM.Models.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbInitializer");

            try
            {
                // Ensure database is created and schema provisioned
                if (context.Database.IsSqlServer())
                {
                    await context.Database.MigrateAsync();
                }
                else
                {
                    await context.Database.EnsureCreatedAsync();
                }

                // 1. Seed Roles
                foreach (var roleName in AppRoles.AllRoles)
                {
                    if (!await roleManager.RoleExistsAsync(roleName))
                    {
                        var role = new ApplicationRole(roleName, $"{roleName} role for AcxiomCRM system.");
                        await roleManager.CreateAsync(role);
                        logger.LogInformation("Seeded Role: {RoleName}", roleName);
                    }
                }

                // 2. Seed Users
                var adminUser = await EnsureUserAsync(
                    userManager,
                    "admin@acxiomcrm.com",
                    "System Administrator",
                    "Admin@12345",
                    AppRoles.Admin,
                    logger);

                var managerUser = await EnsureUserAsync(
                    userManager,
                    "manager@acxiomcrm.com",
                    "Sales Manager",
                    "Manager@12345",
                    AppRoles.Manager,
                    logger);

                var salesUser = await EnsureUserAsync(
                    userManager,
                    "sales@acxiomcrm.com",
                    "Sales Executive",
                    "Sales@12345",
                    AppRoles.SalesExecutive,
                    logger);

                // 3. Seed Sample Customers if empty
                if (!await context.Customers.AnyAsync())
                {
                    var customers = new List<Customer>
                    {
                        new Customer
                        {
                            CustomerCode = "CUST-1001",
                            CustomerName = "Rajesh Sharma",
                            Email = "rajesh.sharma@tataconsultancy.com",
                            Phone = "+91 98200 12345",
                            CompanyName = "Tata Consultancy Services",
                            Address = "TCS Banyan Park, Suren Road, Andheri East",
                            City = "Mumbai",
                            State = "Maharashtra",
                            Status = CustomerStatuses.Active,
                            CreatedDate = DateTime.UtcNow.AddDays(-30),
                            CreatedBy = adminUser.Id,
                            AssignedTo = salesUser.Id
                        },
                        new Customer
                        {
                            CustomerCode = "CUST-1002",
                            CustomerName = "Priya Sundaram",
                            Email = "priya.sundaram@infosys.com",
                            Phone = "+91 98450 67890",
                            CompanyName = "Infosys Limited",
                            Address = "Electronics City, Hosur Road",
                            City = "Bengaluru",
                            State = "Karnataka",
                            Status = CustomerStatuses.Active,
                            CreatedDate = DateTime.UtcNow.AddDays(-20),
                            CreatedBy = managerUser.Id,
                            AssignedTo = salesUser.Id
                        },
                        new Customer
                        {
                            CustomerCode = "CUST-1003",
                            CustomerName = "Vikram Patel",
                            Email = "vikram.patel@wipro.com",
                            Phone = "+91 98790 11223",
                            CompanyName = "Wipro Enterprises",
                            Address = "Sarjapur Road, Doddakannelli",
                            City = "Bengaluru",
                            State = "Karnataka",
                            Status = CustomerStatuses.Active,
                            CreatedDate = DateTime.UtcNow.AddDays(-15),
                            CreatedBy = adminUser.Id,
                            AssignedTo = managerUser.Id
                        }
                    };

                    await context.Customers.AddRangeAsync(customers);
                    await context.SaveChangesAsync();
                    logger.LogInformation("Seeded sample Customers.");
                }

                // 4. Seed Sample Leads if empty
                if (!await context.Leads.AnyAsync())
                {
                    var leads = new List<Lead>
                    {
                        new Lead
                        {
                            LeadCode = "LEAD-2001",
                            LeadName = "Amitabh Sen",
                            Email = "amitabh.sen@reliancedigital.in",
                            Phone = "+91 99300 44556",
                            CompanyName = "Reliance Digital Solutions",
                            Source = "Website",
                            Status = LeadStatuses.Qualified,
                            Priority = "High",
                            ExpectedValue = 450000.00m,
                            CreatedDate = DateTime.UtcNow.AddDays(-14),
                            AssignedTo = salesUser.Id
                        },
                        new Lead
                        {
                            LeadCode = "LEAD-2002",
                            LeadName = "Sunita Rao",
                            Email = "sunita.rao@hcltech.com",
                            Phone = "+91 97110 33445",
                            CompanyName = "HCL Technologies",
                            Source = "Campaign",
                            Status = LeadStatuses.Contacted,
                            Priority = "Medium",
                            ExpectedValue = 280000.00m,
                            CreatedDate = DateTime.UtcNow.AddDays(-10),
                            AssignedTo = salesUser.Id
                        },
                        new Lead
                        {
                            LeadCode = "LEAD-2003",
                            LeadName = "Arjun Kapoor",
                            Email = "arjun.kapoor@mahindratech.com",
                            Phone = "+91 98230 77889",
                            CompanyName = "Tech Mahindra Global",
                            Source = "Referral",
                            Status = LeadStatuses.New,
                            Priority = "High",
                            ExpectedValue = 600000.00m,
                            CreatedDate = DateTime.UtcNow.AddDays(-5),
                            AssignedTo = salesUser.Id
                        }
                    };

                    await context.Leads.AddRangeAsync(leads);
                    await context.SaveChangesAsync();
                    logger.LogInformation("Seeded sample Leads.");
                }

                // 5. Seed Opportunities if empty
                if (!await context.Opportunities.AnyAsync())
                {
                    var cust1 = await context.Customers.FirstAsync(c => c.CustomerCode == "CUST-1001");
                    var cust2 = await context.Customers.FirstAsync(c => c.CustomerCode == "CUST-1002");
                    var lead1 = await context.Leads.FirstAsync(l => l.LeadCode == "LEAD-2001");

                    var opportunities = new List<Opportunity>
                    {
                        new Opportunity
                        {
                            OpportunityName = "Cloud Migration Phase 2",
                            CustomerId = cust1.CustomerId,
                            LeadId = null,
                            Amount = 750000.00m,
                            Stage = OpportunityStages.Proposal,
                            Probability = 60,
                            ExpectedCloseDate = DateTime.UtcNow.AddDays(30),
                            Status = OpportunityStatuses.Open,
                            CreatedDate = DateTime.UtcNow.AddDays(-12),
                            AssignedTo = salesUser.Id
                        },
                        new Opportunity
                        {
                            OpportunityName = "CRM Enterprise License Renewal",
                            CustomerId = cust2.CustomerId,
                            LeadId = null,
                            Amount = 1200000.00m,
                            Stage = OpportunityStages.Negotiation,
                            Probability = 80,
                            ExpectedCloseDate = DateTime.UtcNow.AddDays(15),
                            Status = OpportunityStatuses.Open,
                            CreatedDate = DateTime.UtcNow.AddDays(-8),
                            AssignedTo = salesUser.Id
                        },
                        new Opportunity
                        {
                            OpportunityName = "Omnichannel Retail Integration",
                            CustomerId = null,
                            LeadId = lead1.LeadId,
                            Amount = 450000.00m,
                            Stage = OpportunityStages.Qualification,
                            Probability = 40,
                            ExpectedCloseDate = DateTime.UtcNow.AddDays(45),
                            Status = OpportunityStatuses.Open,
                            CreatedDate = DateTime.UtcNow.AddDays(-4),
                            AssignedTo = salesUser.Id
                        }
                    };

                    await context.Opportunities.AddRangeAsync(opportunities);
                    await context.SaveChangesAsync();
                    logger.LogInformation("Seeded sample Opportunities.");
                }

                // 6. Seed FollowUps if empty
                if (!await context.FollowUps.AnyAsync())
                {
                    var cust1 = await context.Customers.FirstAsync(c => c.CustomerCode == "CUST-1001");
                    var lead2 = await context.Leads.FirstAsync(l => l.LeadCode == "LEAD-2002");

                    var followUps = new List<FollowUp>
                    {
                        new FollowUp
                        {
                            CustomerId = cust1.CustomerId,
                            LeadId = null,
                            FollowUpDate = DateTime.UtcNow.AddDays(2),
                            FollowUpType = FollowUpTypes.Meeting,
                            Remarks = "Discuss architecture and deployment timelines with CTO.",
                            Status = FollowUpStatuses.Planned,
                            AssignedTo = salesUser.Id,
                            CreatedDate = DateTime.UtcNow.AddDays(-2)
                        },
                        new FollowUp
                        {
                            CustomerId = null,
                            LeadId = lead2.LeadId,
                            FollowUpDate = DateTime.UtcNow.AddDays(1),
                            FollowUpType = FollowUpTypes.Call,
                            Remarks = "Follow up on software requirements document and budget approval.",
                            Status = FollowUpStatuses.Planned,
                            AssignedTo = salesUser.Id,
                            CreatedDate = DateTime.UtcNow.AddDays(-1)
                        }
                    };

                    await context.FollowUps.AddRangeAsync(followUps);
                    await context.SaveChangesAsync();
                    logger.LogInformation("Seeded sample FollowUps.");
                }

                // 7. Seed Activities if empty
                if (!await context.Activities.AnyAsync())
                {
                    var cust1 = await context.Customers.FirstAsync(c => c.CustomerCode == "CUST-1001");

                    var activities = new List<Activity>
                    {
                        new Activity
                        {
                            ActivityType = ActivityTypes.Call,
                            Subject = "Initial Discovery Call",
                            Description = "Reviewed high-level system requirements and security concerns.",
                            ActivityDate = DateTime.UtcNow.AddDays(-5),
                            CustomerId = cust1.CustomerId,
                            AssignedTo = salesUser.Id,
                            Status = "Completed",
                            CreatedDate = DateTime.UtcNow.AddDays(-5)
                        },
                        new Activity
                        {
                            ActivityType = ActivityTypes.Meeting,
                            Subject = "Solution Demo Session",
                            Description = "Demonstrated core CRM and dashboard capabilities to project steering committee.",
                            ActivityDate = DateTime.UtcNow.AddDays(-3),
                            CustomerId = cust1.CustomerId,
                            AssignedTo = salesUser.Id,
                            Status = "Completed",
                            CreatedDate = DateTime.UtcNow.AddDays(-3)
                        }
                    };

                    await context.Activities.AddRangeAsync(activities);
                    await context.SaveChangesAsync();
                    logger.LogInformation("Seeded sample Activities.");
                }

                // 8. Seed Audit Log entries if empty
                if (!await context.AuditLogs.AnyAsync())
                {
                    var auditLogs = new List<AuditLog>
                    {
                        new AuditLog
                        {
                            UserId = adminUser.Id,
                            Action = "SystemInitialization",
                            EntityName = "Database",
                            RecordId = "System",
                            Result = "Success",
                            Details = "Database seeded and initial schema provisioned.",
                            CreatedDate = DateTime.UtcNow,
                            IpAddress = "127.0.0.1"
                        }
                    };

                    await context.AuditLogs.AddRangeAsync(auditLogs);
                    await context.SaveChangesAsync();
                    logger.LogInformation("Seeded sample AuditLogs.");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while seeding the database.");
                throw;
            }
        }

        private static async Task<ApplicationUser> EnsureUserAsync(
            UserManager<ApplicationUser> userManager,
            string email,
            string fullName,
            string password,
            string role,
            ILogger logger)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FullName = fullName,
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                };

                var createResult = await userManager.CreateAsync(user, password);
                if (!createResult.Succeeded)
                {
                    var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                    throw new Exception($"Failed to create user {email}: {errors}");
                }

                await userManager.AddToRoleAsync(user, role);
                logger.LogInformation("Created user {Email} with role {Role}", email, role);
            }
            else
            {
                if (!await userManager.IsInRoleAsync(user, role))
                {
                    await userManager.AddToRoleAsync(user, role);
                }
            }

            return user;
        }
    }
}
