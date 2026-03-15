using MealPlanner.Domain.Entities;
using MealPlanner.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MealPlanner.Web.Controllers;

public class UsersController : Controller
{
    private readonly MealPlannerDbContext _context;

    public UsersController(MealPlannerDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var users = await _context.Users
            .OrderBy(x => x.Name)
            .ToListAsync();

        return View(users);
    }

    public async Task<IActionResult> Details(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(x => x.Id == id);

        if (user == null)
        {
            return NotFound();
        }

        return View(user);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(User user)
    {
        if (string.IsNullOrWhiteSpace(user.Email))
        {
            ModelState.AddModelError("Email", "Email is required.");
        }

        if (string.IsNullOrWhiteSpace(user.Name))
        {
            ModelState.AddModelError("Name", "Name is required.");
        }

        if (!ModelState.IsValid)
        {
            return View(user);
        }

        user.Id = Guid.NewGuid();
        user.CreatedAt = DateTime.UtcNow;

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var user = await _context.Users.FindAsync(id);

        if (user == null)
        {
            return NotFound();
        }

        return View(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, User user)
    {
        if (id != user.Id)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(user.Email))
        {
            ModelState.AddModelError("Email", "Email is required.");
        }

        if (string.IsNullOrWhiteSpace(user.Name))
        {
            ModelState.AddModelError("Name", "Name is required.");
        }

        if (!ModelState.IsValid)
        {
            return View(user);
        }

        var userFromDb = await _context.Users.FindAsync(id);

        if (userFromDb == null)
        {
            return NotFound();
        }

        userFromDb.Email = user.Email;
        userFromDb.Name = user.Name;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(x => x.Id == id);

        if (user == null)
        {
            return NotFound();
        }

        return View(user);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var user = await _context.Users.FindAsync(id);

        if (user == null)
        {
            return NotFound();
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}