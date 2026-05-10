using MealPlanner.Api.Models;
using MealPlanner.Domain.Entities;
using MealPlanner.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MealPlanner.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly MealPlannerDbContext _context;

    public UsersController(MealPlannerDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<UserResponse>>> Get()
    {
        var users = await _context.Users
            .OrderBy(x => x.Name)
            .Select(x => ToResponse(x))
            .ToListAsync();

        return Ok(users);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserResponse>> GetById(Guid id)
    {
        var user = await _context.Users.FindAsync(id);

        if (user == null)
        {
            return NotFound();
        }

        return Ok(ToResponse(user));
    }

    [HttpPost]
    public async Task<ActionResult<UserResponse>> Create(CreateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("Email and name are required.");
        }

        var emailExists = await _context.Users.AnyAsync(x => x.Email == request.Email.Trim());

        if (emailExists)
        {
            return BadRequest("User with this email already exists.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email.Trim(),
            Name = request.Name.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = user.Id }, ToResponse(user));
    }

    private static UserResponse ToResponse(User user)
    {
        return new UserResponse
        {
            Id = user.Id,
            Email = user.Email,
            Name = user.Name,
            CreatedAt = user.CreatedAt
        };
    }
}
