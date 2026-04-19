using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using StaffShift.Core.DTOs;
using StaffShift.Core.Entities;
using StaffShift.Services.Interfaces;

namespace StaffShift.Web.Controllers;

/// <summary>
/// Account controller handling user authentication and profile management
/// </summary>
public class AccountController : Controller
{
    private readonly IUserService _userService;
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;

    public AccountController(IUserService userService, UserManager<User> userManager, SignInManager<User> signInManager)
    {
        _userService = userService;
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Dashboard");

        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginDto model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
            return View(model);

        // Support login via email or username
        var usernameOrEmail = model.UsernameOrEmail ?? model.Email ?? string.Empty;

        // Find the user by username or email
        User? user = await _userManager.FindByNameAsync(usernameOrEmail);
        if (user == null)
            user = await _userManager.FindByEmailAsync(usernameOrEmail);

        if (user == null)
        {
            ModelState.AddModelError("", "Invalid username/email or password.");
            return View(model);
        }

        if (!user.IsActive)
        {
            ModelState.AddModelError("", "Your account has been deactivated. Please contact your manager.");
            return View(model);
        }

        // Use SignInManager - this properly sets the Identity cookie
        var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Dashboard");
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError("", "Account locked due to too many failed attempts. Try again in 5 minutes.");
            return View(model);
        }

        ModelState.AddModelError("", "Invalid username/email or password.");
        return View(model);
    }

    // Registration is now handled by CEO/Managers only via CEO/CreateEmployee
    // Public registration is disabled

    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    public async Task<IActionResult> Profile()
    {
        var userId = GetCurrentUserId();
        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null)
            return NotFound();

        return View(user);
    }

    [Authorize]
    public async Task<IActionResult> Edit()
    {
        var userId = GetCurrentUserId();
        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null)
            return NotFound();

        var model = new UpdateProfileDto
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            EmployeeId = user.EmployeeId,
            Department = user.Department,
            Position = user.Position,
            ProfileImageUrl = user.ProfileImageUrl
        };

        ViewBag.Managers = await _userService.GetAllManagersAsync(userId);
        return View(model);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UpdateProfileDto model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Managers = await _userService.GetAllManagersAsync(GetCurrentUserId());
            return View(model);
        }

        var userId = GetCurrentUserId();
        var result = await _userService.UpdateProfileAsync(userId, model);

        if (result.Success)
        {
            TempData["Success"] = result.Message;
            return RedirectToAction(nameof(Profile));
        }

        ModelState.AddModelError("", result.Message);
        ViewBag.Managers = await _userService.GetAllManagersAsync(userId);
        return View(model);
    }

    [Authorize]
    public IActionResult ChangePassword()
    {
        return View();
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordDto model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var userId = GetCurrentUserId();
        var result = await _userService.ChangePasswordAsync(userId, model);

        if (result.Success)
        {
            TempData["Success"] = result.Message;
            return RedirectToAction(nameof(Profile));
        }

        ModelState.AddModelError("", result.Message);
        return View(model);
    }

    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out int userId) ? userId : 0;
    }
}