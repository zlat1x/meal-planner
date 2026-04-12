using System.Security.Claims;
using MealPlanner.Domain.Entities;
using MealPlanner.Infrastructure.Data;
using MealPlanner.Web.Models.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MealPlanner.Web.Controllers;

[AllowAnonymous]
public class AuthController : Controller
{
    private readonly MealPlannerDbContext _context;
    private readonly PasswordHasher<AuthAccount> _passwordHasher;

    public AuthController(MealPlannerDbContext context)
    {
        _context = context;
        _passwordHasher = new PasswordHasher<AuthAccount>();
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var account = await _context.AuthAccounts
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.User.Email == model.Email);

        if (account == null)
        {
            ModelState.AddModelError(string.Empty, "Користувача з таким email не знайдено.");
            return View(model);
        }

        var verifyResult = _passwordHasher.VerifyHashedPassword(account, account.PasswordHash, model.Password);

        if (verifyResult == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError(string.Empty, "Невірний пароль.");
            return View(model);
        }

        await SignInAsync(account);

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Planner");
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var emailExists = await _context.Users.AnyAsync(x => x.Email == model.Email);
        if (emailExists)
        {
            ModelState.AddModelError("Email", "Користувач з таким email уже існує.");
            return View(model);
        }

        var hasAdmin = await _context.AuthAccounts.AnyAsync(x => x.Role == "Admin");
        var role = hasAdmin ? "User" : "Admin";

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = model.Name,
            Email = model.Email,
            CreatedAt = DateTime.UtcNow
        };

        var account = new AuthAccount
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Role = role,
            CreatedAt = DateTime.UtcNow
        };

        account.PasswordHash = _passwordHasher.HashPassword(account, model.Password);

        _context.Users.Add(user);
        _context.AuthAccounts.Add(account);
        await _context.SaveChangesAsync();

        await SignInAsync(account, user);

        return RedirectToAction("Index", "Planner");
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    private async Task SignInAsync(AuthAccount account, User? user = null)
    {
        user ??= account.User;

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, account.Role)
        };

        var claimsIdentity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        var principal = new ClaimsPrincipal(claimsIdentity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
            });
    }
}