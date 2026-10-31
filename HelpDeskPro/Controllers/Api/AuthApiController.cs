using HelpDeskPro.DTOs;
using HelpDeskPro.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HelpDeskPro.Controllers.Api
{
    // JSON version of AccountController, for the React app.
    // It uses the SAME Identity cookie as the MVC pages, so both UIs can run side by side.
    //
    // CSRF: these endpoints only accept application/json. A browser cannot send that cross-site
    // without a CORS pre-flight, and the cookie is SameSite=Lax, so no anti-forgery token is needed
    // (same reasoning as ApiControllerBase).
    [Route("api/auth")]
    public class AuthApiController : ApiControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<AuthApiController> _logger;

        public AuthApiController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<AuthApiController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        // POST /api/auth/login
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<CurrentUserDto>> Login([FromBody] LoginApiRequest request)
        {
            // lockoutOnFailure = true: too many wrong passwords lock the account for a while
            Microsoft.AspNetCore.Identity.SignInResult result =
                await _signInManager.PasswordSignInAsync(request.Email, request.Password, request.RememberMe, true);

            if (result.IsLockedOut)
            {
                return Problem(detail: "Too many failed attempts. Please try again in a few minutes.", statusCode: 423);
            }
            if (!result.Succeeded)
            {
                // Same message for "unknown email" and "wrong password" on purpose
                return Problem(detail: "Invalid email or password.", statusCode: 401);
            }

            ApplicationUser user = await _userManager.FindByEmailAsync(request.Email);
            return Ok(await ToDtoAsync(user));
        }

        // POST /api/auth/register   (always creates a Customer)
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult<CurrentUserDto>> Register([FromBody] RegisterApiRequest request)
        {
            ApplicationUser user = new ApplicationUser();
            user.UserName = request.Email;
            user.Email = request.Email;
            user.FullName = request.FullName;
            user.CreatedAt = DateTime.UtcNow;

            IdentityResult created = await _userManager.CreateAsync(user, request.Password);
            if (!created.Succeeded)
            {
                foreach (IdentityError error in created.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                return ValidationProblem(ModelState);
            }

            await _userManager.AddToRoleAsync(user, Roles.Customer);
            await _signInManager.SignInAsync(user, false);

            _logger.LogInformation("AUDIT: new customer registered {UserId}", user.Id);
            return Ok(await ToDtoAsync(user));
        }

        // POST /api/auth/logout  (always succeeds, even when the cookie already expired)
        [HttpPost("logout")]
        [AllowAnonymous]
        public async Task<ActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return NoContent();
        }

        // GET /api/auth/me   -> 401 when nobody is signed in
        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<CurrentUserDto>> Me()
        {
            ApplicationUser user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                // cookie is valid but the user was deleted
                await _signInManager.SignOutAsync();
                return Problem(detail: "Your session has expired.", statusCode: 401);
            }
            return Ok(await ToDtoAsync(user));
        }

        private async Task<CurrentUserDto> ToDtoAsync(ApplicationUser user)
        {
            IList<string> roles = await _userManager.GetRolesAsync(user);

            string role = Roles.Customer;
            if (roles.Contains(Roles.Admin))
            {
                role = Roles.Admin;
            }
            else if (roles.Contains(Roles.Agent))
            {
                role = Roles.Agent;
            }

            CurrentUserDto dto = new CurrentUserDto();
            dto.UserId = user.Id;
            dto.Email = user.Email;
            dto.FullName = user.FullName;
            dto.Role = role;
            return dto;
        }
    }
}
