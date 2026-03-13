using LuxenHotel.Models.Entities.Identity;
using LuxenHotel.Models.ViewModels.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LuxenHotel.Areas.Customer.Controllers;

[Area("Customer")]
public class IdentityController : Controller
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly RoleManager<Role> _roleManager;

    public IdentityController(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        RoleManager<Role> roleManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity.IsAuthenticated)
        {
            return RedirectToAction("Index", "Home");
        }
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (ModelState.IsValid)
        {
            var existingUser = await _userManager.FindByNameAsync(model.Username);
            if (existingUser != null)
            {
                ModelState.AddModelError("Username", "This username is already taken.");
                return View(model);
            }

            var user = new User
            {
                UserName = model.Username,
                Email = model.Email,
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                // Ensure the Customer role exists
                if (!await _roleManager.RoleExistsAsync("Customer"))
                {
                    await _roleManager.CreateAsync(new Role("Customer"));
                }

                // Assign the Customer role to the newly registered user
                await _userManager.AddToRoleAsync(user, "Customer");

                // Sign in the user
                await _signInManager.SignInAsync(user, isPersistent: false);

                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        return View(model);
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (User.Identity.IsAuthenticated)
        {
            return RedirectToAction("Index", "Home");
        }
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (ModelState.IsValid)
        {
            // Determine if LoginInput is an email or username
            string loginInput = model.UsernameOrEmail;
            User? user = null;

            // Check if the input contains '@' to treat it as an email
            if (loginInput.Contains("@"))
            {
                user = await _userManager.FindByEmailAsync(loginInput);
            }
            else
            {
                user = await _userManager.FindByNameAsync(loginInput);
            }

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "The username/email or password you entered is incorrect. Please try again.");
                return View(model);
            }

            // Check if UserName is null or empty
            if (string.IsNullOrEmpty(user.UserName))
            {
                ModelState.AddModelError(string.Empty, "User account is misconfigured. Please contact support.");
                return View(model);
            }

            // Sign in using the username
            var result = await _signInManager.PasswordSignInAsync(
                user.UserName, model.Password, model.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                // Check roles
                var roles = await _userManager.GetRolesAsync(user);

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction("Index", "Home", new { area = "Customer" });
            }

            ModelState.AddModelError(string.Empty, "The username/email or password you entered is incorrect. Please try again.");
        }

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home", new { area = "Customer" });
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }

        var model = new ProfileViewModel
        {
            Username = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName ?? string.Empty,
            PhoneNumber = user.PhoneNumber
        };

        return View(model);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }

        // Update user properties
        user.FullName = model.FullName;
        user.PhoneNumber = model.PhoneNumber;

        // Email change requires more careful handling since it's used for login
        if (user.Email != model.Email)
        {
            var setEmailResult = await _userManager.SetEmailAsync(user, model.Email);
            if (!setEmailResult.Succeeded)
            {
                foreach (var error in setEmailResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

            // Also update the username to match the email
            var setUsernameResult = await _userManager.SetUserNameAsync(user, model.Email);
            if (!setUsernameResult.Succeeded)
            {
                foreach (var error in setUsernameResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }
        }

        // Save the changes
        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            foreach (var error in updateResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }

        // Refresh sign-in if email changed
        if (user.Email != model.Email)
        {
            await _signInManager.RefreshSignInAsync(user);
        }

        TempData["StatusMessage"] = "Your profile has been updated successfully.";
        return RedirectToAction(nameof(Profile));
    }

    [HttpGet]
    [Authorize]
    public IActionResult ChangePassword()
    {
        return View();
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }

        var changePasswordResult = await _userManager.ChangePasswordAsync(user,
            model.CurrentPassword, model.NewPassword);

        if (!changePasswordResult.Succeeded)
        {
            foreach (var error in changePasswordResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }

        await _signInManager.RefreshSignInAsync(user);
        TempData["StatusMessage"] = "Your password has been changed successfully.";
        return RedirectToAction(nameof(Profile));
    }
}