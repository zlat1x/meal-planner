using MealPlanner.Domain.Entities;
using MealPlanner.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace MealPlanner.Web.Controllers;

public class ProfilesController : Controller
{
    private readonly MealPlannerDbContext _context;

    public ProfilesController(MealPlannerDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var profiles = await _context.Profiles
            .Include(x => x.User)
            .OrderBy(x => x.User.Name)
            .ToListAsync();

        return View(profiles);
    }

    public async Task<IActionResult> Details(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var profile = await _context.Profiles
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (profile == null)
        {
            return NotFound();
        }

        return View(profile);
    }

    public async Task<IActionResult> Create()
    {
        await LoadUsersAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Profile profile)
    {
        ModelState.Remove("User");

        var userExists = await _context.Users.AnyAsync(x => x.Id == profile.UserId);
        if (!userExists)
        {
            ModelState.AddModelError("UserId", "Потрібно вибрати користувача.");
        }

        if (!ModelState.IsValid)
        {
            await LoadUsersAsync(profile.UserId);
            return View(profile);
        }

        profile.Id = Guid.NewGuid();
        profile.UpdatedAt = DateTime.UtcNow;

        _context.Profiles.Add(profile);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var profile = await _context.Profiles.FindAsync(id);

        if (profile == null)
        {
            return NotFound();
        }

        await LoadUsersAsync(profile.UserId);
        return View(profile);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, Profile profile)
    {
        ModelState.Remove("User");

        if (id != profile.Id)
        {
            return NotFound();
        }

        var userExists = await _context.Users.AnyAsync(x => x.Id == profile.UserId);
        if (!userExists)
        {
            ModelState.AddModelError("UserId", "Потрібно вибрати користувача.");
        }

        if (!ModelState.IsValid)
        {
            await LoadUsersAsync(profile.UserId);
            return View(profile);
        }

        var profileFromDb = await _context.Profiles.FindAsync(id);

        if (profileFromDb == null)
        {
            return NotFound();
        }

        profileFromDb.UserId = profile.UserId;
        profileFromDb.WeightKg = profile.WeightKg;
        profileFromDb.Goal = profile.Goal;
        profileFromDb.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var profile = await _context.Profiles
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (profile == null)
        {
            return NotFound();
        }

        return View(profile);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var profile = await _context.Profiles.FindAsync(id);

        if (profile == null)
        {
            return NotFound();
        }

        _context.Profiles.Remove(profile);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    private async Task LoadUsersAsync(Guid? selectedUserId = null)
    {
        var users = await _context.Users
            .OrderBy(x => x.Name)
            .ToListAsync();

        ViewBag.UserId = new SelectList(users, "Id", "Name", selectedUserId);
    }
}