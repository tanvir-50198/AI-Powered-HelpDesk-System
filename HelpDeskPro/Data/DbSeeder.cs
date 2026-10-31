using HelpDeskPro.Models;
using HelpDeskPro.Services.RAG;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskPro.Data
{
    // Fills an empty database with roles, demo users, categories, demo tickets
    // and the starter knowledge documents. Safe to run on every start.
    //
    // DEMO PASSWORDS: change or delete these accounts before any real use!
    public static class DbSeeder
    {
        public const string AdminEmail = "admin@helpdesk.com";
        public const string AdminPassword = "Admin@123";
        public const string AgentEmail = "agent@helpdesk.com";
        public const string AgentPassword = "Agent@123";
        public const string CustomerEmail = "customer@helpdesk.com";
        public const string CustomerPassword = "Customer@123";

        public static async Task SeedAsync(IServiceProvider services)
        {
            ApplicationDbContext db = services.GetRequiredService<ApplicationDbContext>();
            UserManager<ApplicationUser> userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            RoleManager<IdentityRole> roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            IKnowledgeSeeder knowledgeSeeder = services.GetRequiredService<IKnowledgeSeeder>();

            await SeedRolesAsync(roleManager);

            ApplicationUser admin = await SeedUserAsync(userManager, AdminEmail, AdminPassword, "Demo Admin", Roles.Admin);
            ApplicationUser agent = await SeedUserAsync(userManager, AgentEmail, AgentPassword, "Demo Agent", Roles.Agent);
            ApplicationUser customer = await SeedUserAsync(userManager, CustomerEmail, CustomerPassword, "Demo Customer", Roles.Customer);

            await SeedCategoriesAsync(db);
            await SeedDemoTicketsAsync(db, admin, agent, customer);

            await knowledgeSeeder.ImportKnowledgeBaseFolderAsync();
        }

        private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            string[] roleNames = { Roles.Customer, Roles.Agent, Roles.Admin };
            foreach (string roleName in roleNames)
            {
                bool exists = await roleManager.RoleExistsAsync(roleName);
                if (!exists)
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }
        }

        private static async Task<ApplicationUser> SeedUserAsync(UserManager<ApplicationUser> userManager, string email, string password, string fullName, string role)
        {
            ApplicationUser user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new ApplicationUser();
                user.UserName = email;
                user.Email = email;
                user.EmailConfirmed = true;
                user.FullName = fullName;
                user.CreatedAt = DateTime.UtcNow;

                IdentityResult result = await userManager.CreateAsync(user, password);
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException("Could not create demo user " + email);
                }
                await userManager.AddToRoleAsync(user, role);
            }
            return user;
        }

        private static async Task SeedCategoriesAsync(ApplicationDbContext db)
        {
            if (await db.Categories.AnyAsync())
            {
                return;
            }

            db.Categories.Add(new Category { Name = "General Question", Description = "Anything that does not fit another category" });
            db.Categories.Add(new Category { Name = "Account & Login", Description = "Password, registration and access problems" });
            db.Categories.Add(new Category { Name = "Technical Issue", Description = "Errors, bugs and unexpected behaviour" });
            db.Categories.Add(new Category { Name = "Billing", Description = "Invoices and payments" });
            db.Categories.Add(new Category { Name = "Feature Request", Description = "Ideas for improvements" });
            await db.SaveChangesAsync();
        }

        // A few sample tickets so the dashboards are not empty on the first start
        private static async Task SeedDemoTicketsAsync(ApplicationDbContext db, ApplicationUser admin, ApplicationUser agent, ApplicationUser customer)
        {
            if (await db.Tickets.AnyAsync())
            {
                return;
            }

            Category accountCategory = await db.Categories.FirstAsync(c => c.Name == "Account & Login");
            Category technicalCategory = await db.Categories.FirstAsync(c => c.Name == "Technical Issue");
            Category generalCategory = await db.Categories.FirstAsync(c => c.Name == "General Question");

            Ticket first = new Ticket();
            first.Title = "[Demo] I cannot log in after changing my password";
            first.Description = "I changed my password yesterday and now the login page says my password is wrong.";
            first.CategoryId = accountCategory.Id;
            first.Priority = TicketPriority.High;
            first.Status = TicketStatus.InProgress;
            first.CreatedByUserId = customer.Id;
            first.AssignedAgentId = agent.Id;
            first.CreatedAt = DateTime.UtcNow.AddHours(-5);
            first.UpdatedAt = DateTime.UtcNow.AddHours(-1);

            Ticket second = new Ticket();
            second.Title = "[Demo] The ticket page shows an error 500";
            second.Description = "When I open my ticket list I sometimes see a server error.";
            second.CategoryId = technicalCategory.Id;
            second.Priority = TicketPriority.Medium;
            second.Status = TicketStatus.Open;
            second.CreatedByUserId = customer.Id;
            second.CreatedAt = DateTime.UtcNow.AddDays(-3);
            second.UpdatedAt = DateTime.UtcNow.AddDays(-3);

            Ticket third = new Ticket();
            third.Title = "[Demo] How do I export my ticket list?";
            third.Description = "Is there a way to download all my tickets as a file?";
            third.CategoryId = generalCategory.Id;
            third.Priority = TicketPriority.Low;
            third.Status = TicketStatus.Resolved;
            third.CreatedByUserId = customer.Id;
            third.AssignedAgentId = agent.Id;
            third.ResolutionDetails = "Export is not available yet. We added your idea to the feature list.";
            third.CreatedAt = DateTime.UtcNow.AddDays(-2);
            third.UpdatedAt = DateTime.UtcNow.AddDays(-1);
            third.ResolvedAt = DateTime.UtcNow.AddDays(-1);

            db.Tickets.Add(first);
            db.Tickets.Add(second);
            db.Tickets.Add(third);
            await db.SaveChangesAsync();

            db.TicketHistory.Add(new TicketHistory { TicketId = first.Id, ActorUserId = customer.Id, FieldName = "Ticket", NewValue = "Created", CreatedAt = first.CreatedAt });
            db.TicketHistory.Add(new TicketHistory { TicketId = first.Id, ActorUserId = admin.Id, FieldName = "Assigned agent", OldValue = "Unassigned", NewValue = agent.Email, CreatedAt = first.CreatedAt.AddHours(1) });
            db.TicketHistory.Add(new TicketHistory { TicketId = second.Id, ActorUserId = customer.Id, FieldName = "Ticket", NewValue = "Created", CreatedAt = second.CreatedAt });
            db.TicketHistory.Add(new TicketHistory { TicketId = third.Id, ActorUserId = customer.Id, FieldName = "Ticket", NewValue = "Created", CreatedAt = third.CreatedAt });
            db.TicketHistory.Add(new TicketHistory { TicketId = third.Id, ActorUserId = agent.Id, FieldName = "Status", OldValue = "InProgress", NewValue = "Resolved", CreatedAt = third.ResolvedAt.Value });

            db.TicketComments.Add(new TicketComment { TicketId = first.Id, AuthorUserId = agent.Id, Body = "Hello! Please try the 'Forgot password' link once more and tell me what happens.", CreatedAt = DateTime.UtcNow.AddHours(-1) });
            await db.SaveChangesAsync();
        }
    }
}
