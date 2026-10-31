using HelpDeskPro.Models;
using HelpDeskPro.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HelpDeskPro.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<AccountController> _logger;

        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        // ---------------- Register ----------------
        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            ApplicationUser user = new ApplicationUser();
            user.UserName = model.Email;
            user.Email = model.Email;
            user.FullName = model.FullName;
            user.CreatedAt = DateTime.UtcNow;

            IdentityResult result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                foreach (IdentityError error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                return View(model);
            }

            // Everybody who registers on the website is a Customer.
            // Agents and admins are created by an admin (Admin > Users).
            await _userManager.AddToRoleAsync(user, Roles.Customer);

            await _signInManager.SignInAsync(user, false);
            return RedirectToAction("Index", "Home");
        }

        // ---------------- Login ----------------
        [HttpGet]
        public IActionResult Login(string returnUrl)
        {
            LoginViewModel model = new LoginViewModel();
            model.ReturnUrl = returnUrl;
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // lockoutOnFailure = true: too many wrong passwords lock the account for a while
            Microsoft.AspNetCore.Identity.SignInResult result =
                await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, true);

            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                {
                    return Redirect(model.ReturnUrl);
                }
                return RedirectToAction("Index", "Home");
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError("", "Too many failed attempts. Please try again in a few minutes.");
            }
            else
            {
                // Same message for "unknown email" and "wrong password" on purpose
                ModelState.AddModelError("", "Invalid email or password.");
            }
            return View(model);
        }

        // ---------------- Logout ----------------
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
