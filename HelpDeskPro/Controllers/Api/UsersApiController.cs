using HelpDeskPro.Data;
using HelpDeskPro.DTOs;
using HelpDeskPro.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskPro.Controllers.Api
{
    // User management for admins (JSON version of AdminController.Users / ChangeRole).
    [Route("api/admin")]
    [Authorize(Roles = Roles.Admin)]
    public class UsersApiController : ApiControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _db;
        private readonly ILogger<UsersApiController> _logger;

        public UsersApiController(UserManager<ApplicationUser> userManager, ApplicationDbContext db, ILogger<UsersApiController> logger)
        {
            _userManager = userManager;
            _db = db;
            _logger = logger;
        }

        // GET /api/admin/users
        [HttpGet("users")]
        public async Task<ActionResult<List<AdminUserDto>>> GetUsers()
        {
            List<ApplicationUser> users = await _userManager.Users.OrderBy(u => u.Email).ToListAsync();

            // one query for all roles (instead of one query per user)
            var roleRows = await (from userRole in _db.UserRoles
                                  join role in _db.Roles on userRole.RoleId equals role.Id
                                  select new { userRole.UserId, RoleName = role.Name }).ToListAsync();

            Dictionary<string, string> roleByUser = new Dictionary<string, string>();
            foreach (var row in roleRows)
            {
                // if somebody has several roles, the strongest one wins
                if (!roleByUser.ContainsKey(row.UserId) || Strength(row.RoleName) > Strength(roleByUser[row.UserId]))
                {
                    roleByUser[row.UserId] = row.RoleName;
                }
            }

            List<AdminUserDto> list = new List<AdminUserDto>();
            foreach (ApplicationUser user in users)
            {
                AdminUserDto dto = new AdminUserDto();
                dto.Id = user.Id;
                dto.Email = user.Email;
                dto.FullName = user.FullName;
                dto.CreatedAt = user.CreatedAt;
                dto.Role = roleByUser.ContainsKey(user.Id) ? roleByUser[user.Id] : Roles.Customer;
                list.Add(dto);
            }
            return Ok(list);
        }

        // GET /api/admin/agents   (used by "assign ticket")
        [HttpGet("agents")]
        public async Task<ActionResult<List<AgentOptionDto>>> GetAgents()
        {
            IList<ApplicationUser> agents = await _userManager.GetUsersInRoleAsync(Roles.Agent);

            List<AgentOptionDto> list = new List<AgentOptionDto>();
            foreach (ApplicationUser agent in agents.OrderBy(a => a.Email))
            {
                AgentOptionDto dto = new AgentOptionDto();
                dto.Id = agent.Id;
                dto.Email = agent.Email;
                dto.FullName = agent.FullName;
                list.Add(dto);
            }
            return Ok(list);
        }

        // PATCH /api/admin/users/{id}/role
        [HttpPatch("users/{userId}/role")]
        public async Task<ActionResult> ChangeRole(string userId, [FromBody] ChangeRoleRequest request)
        {
            if (request.Role != Roles.Customer && request.Role != Roles.Agent && request.Role != Roles.Admin)
            {
                return Problem(detail: "Invalid role.", statusCode: 400);
            }

            ApplicationUser user = await _userManager.FindByIdAsync(userId ?? "");
            if (user == null)
            {
                return Problem(detail: "User not found.", statusCode: 404);
            }

            // An admin cannot remove his own admin rights by accident
            if (user.Id == CurrentUser.UserId)
            {
                return Problem(detail: "You cannot change your own role.", statusCode: 409);
            }

            IList<string> oldRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, oldRoles);
            await _userManager.AddToRoleAsync(user, request.Role);

            _logger.LogInformation("AUDIT: admin {AdminId} changed role of user {UserId} to {Role}", CurrentUser.UserId, user.Id, request.Role);
            return NoContent();
        }

        private static int Strength(string role)
        {
            if (role == Roles.Admin)
            {
                return 3;
            }
            if (role == Roles.Agent)
            {
                return 2;
            }
            return 1;
        }
    }
}
