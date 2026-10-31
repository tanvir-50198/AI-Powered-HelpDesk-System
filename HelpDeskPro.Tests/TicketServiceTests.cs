using HelpDeskPro.Data;
using HelpDeskPro.Models;
using HelpDeskPro.Services.Common;
using HelpDeskPro.Services.Tickets;
using Xunit;

namespace HelpDeskPro.Tests
{
    public class TicketServiceTests
    {
        // UserManager is only needed for AssignAgentAsync, so tests pass null here
        private TicketService CreateService(ApplicationDbContext db)
        {
            return new TicketService(db, null);
        }

        private async Task<Ticket> CreateTicketAsync(TicketService service, string customerId)
        {
            ServiceResult<Ticket> result = await service.CreateTicketAsync(
                TestHelper.Customer(customerId), "Printer problem", "It does not print", 1, TicketPriority.Medium);
            return result.Data;
        }

        [Fact]
        public async Task Customer_CanCreateTicket_AndHistoryIsWritten()
        {
            ApplicationDbContext db = TestHelper.CreateDatabase();
            TicketService service = CreateService(db);

            Ticket ticket = await CreateTicketAsync(service, "customer1");

            Assert.True(ticket.Id > 0);
            Assert.Equal(TicketStatus.Open, ticket.Status);
            Assert.Equal(1, db.TicketHistory.Count(h => h.TicketId == ticket.Id));
        }

        [Fact]
        public async Task Customer_CannotReadTicketOfAnotherCustomer()
        {
            ApplicationDbContext db = TestHelper.CreateDatabase();
            TicketService service = CreateService(db);
            Ticket ticket = await CreateTicketAsync(service, "customer1");

            ServiceResult<Ticket> result = await service.GetTicketAsync(ticket.Id, TestHelper.Customer("customer2"));

            Assert.False(result.Success);
            Assert.Equal(ErrorType.Forbidden, result.ErrorType);
        }

        [Fact]
        public async Task Customer_ListContainsOnlyOwnTickets()
        {
            ApplicationDbContext db = TestHelper.CreateDatabase();
            TicketService service = CreateService(db);
            await CreateTicketAsync(service, "customer1");
            await CreateTicketAsync(service, "customer2");

            PagedResult<Ticket> list = await service.GetTicketsAsync(TestHelper.Customer("customer1"), new TicketFilter());

            Assert.Equal(1, list.TotalCount);
            Assert.Equal("customer1", list.Items[0].CreatedByUserId);
        }

        [Fact]
        public async Task Customer_CannotChangeStatus()
        {
            ApplicationDbContext db = TestHelper.CreateDatabase();
            TicketService service = CreateService(db);
            Ticket ticket = await CreateTicketAsync(service, "customer1");

            ServiceResult result = await service.UpdateStatusAsync(ticket.Id, TestHelper.Customer("customer1"), TicketStatus.Closed);

            Assert.False(result.Success);
            Assert.Equal(ErrorType.Forbidden, result.ErrorType);
        }

        [Fact]
        public async Task Agent_CannotUpdateUnassignedTicket()
        {
            ApplicationDbContext db = TestHelper.CreateDatabase();
            TicketService service = CreateService(db);
            Ticket ticket = await CreateTicketAsync(service, "customer1");

            ServiceResult result = await service.UpdateStatusAsync(ticket.Id, TestHelper.Agent("agent1"), TicketStatus.InProgress);

            Assert.False(result.Success);
            Assert.Equal(ErrorType.Forbidden, result.ErrorType);
        }

        [Fact]
        public async Task Agent_CanUpdateAssignedTicket_AndHistoryIsWritten()
        {
            ApplicationDbContext db = TestHelper.CreateDatabase();
            TicketService service = CreateService(db);
            Ticket ticket = await CreateTicketAsync(service, "customer1");
            ticket.AssignedAgentId = "agent1";
            await db.SaveChangesAsync();

            ServiceResult result = await service.UpdateStatusAsync(ticket.Id, TestHelper.Agent("agent1"), TicketStatus.InProgress);

            Assert.True(result.Success);
            Assert.True(db.TicketHistory.Any(h => h.TicketId == ticket.Id && h.FieldName == "Status" && h.NewValue == "InProgress"));
        }

        [Fact]
        public async Task Resolving_NeedsResolutionDetails()
        {
            ApplicationDbContext db = TestHelper.CreateDatabase();
            TicketService service = CreateService(db);
            Ticket ticket = await CreateTicketAsync(service, "customer1");
            ticket.AssignedAgentId = "agent1";
            await db.SaveChangesAsync();
            UserContext agent = TestHelper.Agent("agent1");

            ServiceResult withoutText = await service.UpdateStatusAsync(ticket.Id, agent, TicketStatus.Resolved);
            await service.SaveResolutionAsync(ticket.Id, agent, "Replaced the cable.");
            ServiceResult withText = await service.UpdateStatusAsync(ticket.Id, agent, TicketStatus.Resolved);

            Assert.False(withoutText.Success);
            Assert.Equal(ErrorType.Validation, withoutText.ErrorType);
            Assert.True(withText.Success);
        }

        [Fact]
        public async Task Comment_CreatesHistory_AndEmptyCommentIsRejected()
        {
            ApplicationDbContext db = TestHelper.CreateDatabase();
            TicketService service = CreateService(db);
            Ticket ticket = await CreateTicketAsync(service, "customer1");

            ServiceResult good = await service.AddCommentAsync(ticket.Id, TestHelper.Customer("customer1"), "More details");
            ServiceResult empty = await service.AddCommentAsync(ticket.Id, TestHelper.Customer("customer1"), "   ");

            Assert.True(good.Success);
            Assert.False(empty.Success);
            Assert.Equal(1, db.TicketComments.Count());
            Assert.True(db.TicketHistory.Any(h => h.FieldName == "Comment"));
        }

        [Fact]
        public async Task InvalidInput_IsRejected()
        {
            ApplicationDbContext db = TestHelper.CreateDatabase();
            TicketService service = CreateService(db);

            ServiceResult<Ticket> noTitle = await service.CreateTicketAsync(TestHelper.Customer("customer1"), "", "text", 1, TicketPriority.Low);
            ServiceResult<Ticket> badCategory = await service.CreateTicketAsync(TestHelper.Customer("customer1"), "Title", "text", 999, TicketPriority.Low);
            ServiceResult<Ticket> badPriority = await service.CreateTicketAsync(TestHelper.Customer("customer1"), "Title", "text", 1, (TicketPriority)99);

            Assert.Equal(ErrorType.Validation, noTitle.ErrorType);
            Assert.Equal(ErrorType.Validation, badCategory.ErrorType);
            Assert.Equal(ErrorType.Validation, badPriority.ErrorType);
        }

        [Fact]
        public async Task OnlyAdmin_CanAssign()
        {
            ApplicationDbContext db = TestHelper.CreateDatabase();
            TicketService service = CreateService(db);
            Ticket ticket = await CreateTicketAsync(service, "customer1");

            ServiceResult result = await service.AssignAgentAsync(ticket.Id, TestHelper.Agent("agent1"), "agent1");

            Assert.Equal(ErrorType.Forbidden, result.ErrorType);
        }

        [Fact]
        public async Task ClosedTicket_DoesNotAcceptComments_AndAgentCannotReopen()
        {
            ApplicationDbContext db = TestHelper.CreateDatabase();
            TicketService service = CreateService(db);
            Ticket ticket = await CreateTicketAsync(service, "customer1");
            ticket.AssignedAgentId = "agent1";
            ticket.ResolutionDetails = "Done";
            ticket.Status = TicketStatus.Closed;
            await db.SaveChangesAsync();

            ServiceResult comment = await service.AddCommentAsync(ticket.Id, TestHelper.Customer("customer1"), "Hello?");
            ServiceResult reopen = await service.UpdateStatusAsync(ticket.Id, TestHelper.Agent("agent1"), TicketStatus.InProgress);
            ServiceResult adminReopen = await service.UpdateStatusAsync(ticket.Id, TestHelper.Admin(), TicketStatus.InProgress);

            Assert.Equal(ErrorType.Conflict, comment.ErrorType);
            Assert.Equal(ErrorType.Conflict, reopen.ErrorType);
            Assert.True(adminReopen.Success);
        }
    }
}
