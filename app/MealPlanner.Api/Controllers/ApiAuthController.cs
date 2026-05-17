using MealPlanner.Api.Models;
using MealPlanner.Domain.Entities;
using MealPlanner.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MealPlanner.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class ApiAuthController : ControllerBase
{
    private readonly MealPlannerDbContext _context;
    private readonly PasswordHasher<AuthAccount> _passwordHasher;

    public ApiAuthController(MealPlannerDbContext context)
    {
        _context = context;
        _passwordHasher = new PasswordHasher<AuthAccount>();
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiAuthResponse>> Login(ApiLoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new ApiAuthResponse
            {
                Success = false,
                Message = "Email and password are required."
            });
        }

        var account = await _context.AuthAccounts
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.User.Email == request.Email.Trim());

        if (account == null)
        {
            return Unauthorized(new ApiAuthResponse
            {
                Success = false,
                Message = "User with this email was not found."
            });
        }

        var verifyResult = _passwordHasher.VerifyHashedPassword(
            account,
            account.PasswordHash,
            request.Password);

        if (verifyResult == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new ApiAuthResponse
            {
                Success = false,
                Message = "Password is incorrect."
            });
        }

        return Ok(new ApiAuthResponse
        {
            Success = true,
            Message = "Login successful.",
            UserId = account.UserId,
            UserName = account.User.Name,
            Email = account.User.Email,
            Role = account.Role
        });
    }

    [HttpPost("register")]
    public async Task<ActionResult<ApiAuthResponse>> Register(ApiRegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name)
            || string.IsNullOrWhiteSpace(request.Email)
            || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new ApiAuthResponse
            {
                Success = false,
                Message = "Name, email and password are required."
            });
        }

        var email = request.Email.Trim();
        var emailExists = await _context.Users.AnyAsync(x => x.Email == email);

        if (emailExists)
        {
            return BadRequest(new ApiAuthResponse
            {
                Success = false,
                Message = "User with this email already exists."
            });
        }

        var hasAdmin = await _context.AuthAccounts.AnyAsync(x => x.Role == "Admin");
        var role = hasAdmin ? "User" : "Admin";

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Email = email,
            CreatedAt = DateTime.UtcNow
        };

        var account = new AuthAccount
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Role = role,
            CreatedAt = DateTime.UtcNow
        };

        account.PasswordHash = _passwordHasher.HashPassword(account, request.Password);

        _context.Users.Add(user);
        _context.AuthAccounts.Add(account);
        await _context.SaveChangesAsync();

        return Ok(new ApiAuthResponse
        {
            Success = true,
            Message = "Registration successful.",
            UserId = user.Id,
            UserName = user.Name,
            Email = user.Email,
            Role = role
        });
    }
}
